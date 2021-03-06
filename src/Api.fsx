
#I @"../packages/FSharp.Data/lib/net40/"
#r "FSharp.Data.DesignTime.dll"
#r "FSharp.Data.dll"
// #load "../Provider.fsx"

open System
open System.Linq
open FSharp.Data
open System.IO
// open Provider

module Api =

    type SlUsers = JsonProvider<"./data/json/users.json">
    type SlTracks = JsonProvider<"./data/json/tracks.json">
    type SlUserTracks = JsonProvider<"./data/json/userTracks.json">

    type Screen() =
        static member Info s1 s2 =
            let smg = sprintf "%s -> %s" s1 s2
            Console.WriteLine smg

    type Config = {
        ClientId: string
        ClientSecret: string
        EndUserAuthentication: string
        Token: string
    }

    type ConfigLoader() =
        member this.Default() =
            { Config.ClientId = "6abc49dd6e4bf58c4d8829def2260ec9"
              ClientSecret = "1b148a720c7d105b0fdae5a3120efff8"
              EndUserAuthentication = "https://soundcloud.com/connect"
              Token = "https://api.soundcloud.com/oauth2/token" }

    type SlApi(config: Config) =
        let baseUrl = "http://api.soundcloud.com"
        let clientId = sprintf "client_id=%s" config.ClientId

        member this.UserTracks(user: string) =
            let url = sprintf "%s/users/%s/tracks?%s" baseUrl user clientId
            url |> Http.RequestString |> SlUserTracks.Parse

        member this.Tracks(track: string) =
            sprintf "%s/tracks/%s?%s" baseUrl track clientId
            |> Http.RequestString |> SlTracks.Parse

        member this.Users(user: string) =
            sprintf "%s/users/%s?%s" baseUrl user clientId
            |> Http.RequestString |> SlUsers.Parse

        member this.GetArtworksUrl (tracks: SlUserTracks.Root[]) =
            tracks|> Seq.map (fun x -> (x.ArtworkUrl, sprintf "%s.png" x.Permalink))

        member this.GetStreamsUrl (tracks: SlUserTracks.Root[]) =
            tracks |> Seq.map(fun x -> (sprintf "%s?%s" x.StreamUrl clientId, sprintf "%s.mp3" x.Permalink))

    type Runner(config: Config) =

        let api = SlApi(config)

        let createDir target =
            if not (Directory.Exists target) then
                Directory.CreateDirectory target
            else DirectoryInfo target

        member private this.DownloadFile(url: string, target: string) =
            use stream = new FileStream(target,FileMode.Open, FileAccess.Write)
            Http.RequestStream(url).ResponseStream.CopyTo stream

        member private this.Download (target: string) (url: string , label: string) =
            async {
                Screen.Info "|| downloading" label
                let path = Path.Combine(target, label)
                match Http.Request(url).Body with
                | Text text -> ()
                | Binary bin ->  File.WriteAllBytes(path, bin)
            }
        member this.DownloadStreams(target: string, ?user: string) =
            let usr = defaultArg user "jannina-weigel"
            let streams = api.UserTracks usr |> api.GetStreamsUrl
            createDir target |> ignore

            streams
            |> Seq.map (fun x -> this.Download target x)
            |> Async.Parallel
            |> Async.RunSynchronously
            |> ignore

        member this.DownloadArtworks(target: string, ?user: string) =
            let usr = defaultArg user "jannina-weigel"
            let artkworks = api.UserTracks usr |> api.GetArtworksUrl
            createDir target |> ignore

            artkworks
            |> Seq.map (fun x -> this.Download target (x))
            |> Async.Parallel
            |> Async.RunSynchronously
            |> ignore
