#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "packages/FsPickler/lib/net40/FsPickler.dll"
#r "packages/FSharp.Compiler.Service/lib/net45/FSharp.Compiler.Service.dll"
#r "packages/Chessie/lib/net40/Chessie.dll"
#r "packages/Paket.Core/lib/net45/Paket.Core.dll"
#r "packages/Suave/lib/net40/Suave.dll"
#load "packages/FSharp.Formatting/FSharp.Formatting.fsx"
#load "paket-files/raw.githubusercontent.com/utils.fs"
#load "packages/FSharp.Azure.StorageTypeProvider/StorageTypeProvider.fsx"
#load "config.fsx"
open FSharp.Data
open FSharp.Azure.StorageTypeProvider
open Microsoft.WindowsAzure.Storage.Blob
open System.IO

// -------------------------------------------------------------------------------------------------
// Various scripts for removing malicious edits
// -------------------------------------------------------------------------------------------------

// Initialize Azure Storage from example data
type Azure = AzureTypeProvider<Config.azureConnectionString>
type Index = JsonProvider<"samples/index.json">

let index = Azure.Containers.data.``index.json``.Read()
let parsed = Index.Parse(index)

let original = Index.Load(__SOURCE_DIRECTORY__ + "/data/index.json")
let originalLookup = dict [ for s in original.Snippets -> s.Id, s ]

// Find snippets that have been modified
parsed.Snippets
|> Seq.filter (fun s ->
    originalLookup.ContainsKey s.Id && s.Date <> originalLookup.[s.Id].Date)
|> Seq.iter (fun s -> printfn "%d/%s %s" s.Id (FsSnip.Utils.mangleId s.Id) s.Title)

// Spambot inserted lots of crap, but always put '=' at the end of passcode :)
let isSpam (snippet:Index.Snippet) = 
  snippet.Passcode.EndsWith "=" && snippet.Id > 1850

// Those earlier snippets were also edited by the spambot
let restoreIds = 
  set [ 51; 1679; 1474; 1679 ]

let newSnippets = 
  parsed.Snippets
  |> Seq.filter (isSpam >> not)
  |> Seq.map (fun s ->
      if restoreIds.Contains s.Id then originalLookup.[s.Id] else s)
  |> Seq.toArray

let newIndex = Index.Root(newSnippets)
let indexFixed = Path.Combine(Path.GetTempPath(), "index.json")
File.WriteAllText(indexFixed, newIndex.JsonValue.ToString())

Azure.Containers.data.``index.json``.AsICloudBlob().Delete()
Azure.Containers.data.Upload(indexFixed) |> Async.RunSynchronously

// -------------------------------------------------------------------------------------------------
// Garbage collect unreferenced snippets
// -------------------------------------------------------------------------------------------------

let gcIndex = Azure.Containers.data.``index.json``.Read()
let gcLookup = dict [ for s in Index.Parse(gcIndex).Snippets -> s.Id, s.Versions ]

let dataContainer = Azure.Containers.data.AsCloudBlobContainer()

// Delete all blobs with ID that is not referenced from the snippet index
let gcBlobs folder = 
  let blobs = dataContainer.GetDirectoryReference(folder + "/").ListBlobs() |> Array.ofSeq
  for b in blobs do
    let id = b.Uri.Segments.[b.Uri.Segments.Length-1]
    let id = id.Substring(0, id.Length-1) |> int
    if not (gcLookup.ContainsKey id) then
      let childs = dataContainer.GetDirectoryReference(sprintf "%s/%d" folder id).ListBlobs() |> Array.ofSeq
      for c in childs do
        let ver = c.Uri.Segments.[c.Uri.Segments.Length-1]
        let ver = ver |> int
        let blob = dataContainer.GetBlobReference(sprintf "%s/%d/%d" folder id ver)
        printfn "Deleting: %O" blob.Uri
        blob.Delete()

gcBlobs "source"
gcBlobs "formatted"







