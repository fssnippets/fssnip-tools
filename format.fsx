// -------------------------------------------------------------------------------------------------
// Formatting raw snippets offline
// -------------------------------------------------------------------------------------------------

#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "packages/FsPickler/lib/net40/FsPickler.dll"
#r "packages/Paket/tools/paket.exe"
#load "packages/FSharp.Formatting/FSharp.Formatting.fsx"
open System
open System.IO
open FSharp.Literate
open FSharp.CodeFormat
open Nessos.FsPickler
open FSharp.Data

type Index = JsonProvider<"data/index.json">
let snippets = Index.GetSample().Snippets
let data = __SOURCE_DIRECTORY__ + "/data"

let formatAgent = CodeFormat.CreateAgent()
let pickler = FsPickler.CreateXml()

let formatSnippet (parsed:string) (formatted:string) source nuget = 
  let doc = Literate.ParseScriptString(source, "/Snippet.fsx", formatAgent)  
  let html = Literate.WriteHtml(doc, "fs", true, true)
  File.WriteAllText(formatted, html)
  use wr = new StreamWriter(parsed)
  pickler.Serialize(wr, doc)  

for s in snippets do 
  printfn "Processing snippet: %d" s.Id
  for v in 0 .. s.Versions - 1 do
    let source = File.ReadAllText(sprintf "%s/source/%d/%d" data s.Id v)
    let dir = sprintf "%s/parsed/%d" data s.Id 
    if not (Directory.Exists(dir)) then Directory.CreateDirectory(dir) |> ignore
    let dir = sprintf "%s/formatted/%d" data s.Id 
    if not (Directory.Exists(dir)) then Directory.CreateDirectory(dir) |> ignore
    let formattedFile = sprintf "%s/formatted/%d/%d" data s.Id v
    let parsedFile = sprintf "%s/parsed/%d/%d" data s.Id v
    if not (File.Exists(formattedFile)) || not (File.Exists(parsedFile)) then
      formatSnippet parsedFile formattedFile source s.References

