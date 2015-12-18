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
open FSharp.Markdown
open FSharp.Data

type Index = JsonProvider<"data/index.json">
let snippets = Index.GetSample().Snippets
let data = __SOURCE_DIRECTORY__ + "/data"

let formatAgent = CodeFormat.CreateAgent()
let pickler = FsPickler.CreateXmlSerializer()

let formatSnippet (parsed:string) (formatted:string) source nuget = 
  let doc = Literate.ParseScriptString(source, "/Snippet.fsx", formatAgent)  
  let html = Literate.WriteHtml(doc, "fs", true, true)
  File.WriteAllText(formatted, html)
  use wr = new StreamWriter(parsed)
  pickler.Serialize(wr, doc)  

let processSnippets () =
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


// Pick one of the snippets
let snippet = snippets |> Seq.nth 0
// Pick version in the range [0 .. snippet.Versions - 1]
let version = 0 

// Read the pickled version so that we don't have to re-parse it
printfn "Reading snippet: %d/%d" snippet.Id version
let parsedFile = sprintf "%s/parsed/%d/%d" data snippet.Id version
let rd = new StreamReader(parsedFile)
let doc = pickler.Deserialize<LiterateDocument>(rd)  

for p in doc.Paragraphs do
  match p with
  | Matching.LiterateParagraph(FormattedCode lines) ->
      // Parsed and type checked F# code as a sequence of lines
      for (Line tokens) in lines do
        for token in tokens do
          match token with
          | TokenSpan.Token(kind, s, tip) ->
              // Ordinary F# langauge token
              printf "%s" s
              match tip with
              | Some tip -> 
                  // This token has an associated tool-tip which shows
                  // its type and other documentation (when available)
                  printf "(^)"
              | None -> ()
          | _ ->
              // There are some other cases here for "omitted" code, 
              // F# interactive output and errors. I think only omitted
              // may appear in fssnips, but not very often
              ()
        printfn ""

  | Heading(n, s) ->
      // Snippet may contain heading (when there are multiple code blocks, e.g. http://fssnip.net/m)
      printfn "Heading %d: %A" n s
      
  | _ -> failwith "Unexpected content"

