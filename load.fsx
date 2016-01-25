#r "packages/SQLProvider/lib/FSharp.Data.SqlProvider.dll"
#r "packages/Suave/lib/net40/Suave.dll"
#load "packages/FSharp.Formatting/FSharp.Formatting.fsx"
#load "paket-files/raw.githubusercontent.com/utils.fs"
open System
open System.IO
open FSharp.Data.Sql
open FsSnip.Utils

// -------------------------------------------------------------------------------------------------
// Reading F# snippets from database used by the old version
// -------------------------------------------------------------------------------------------------

type FsSnip = 
  SqlDataProvider<ConnectionString = "Data Source=.\\SQLExpress;Initial catalog=fssnip;Integrated security=true",
                  DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER>
let ctx = FsSnip.GetDataContext()

let versions = 
  [ for v in ctx.Dbo.Versions -> 
      v.CodeId, v.VersionNumber, (v.Formatted, v.Source) ]
  |> Seq.groupBy (fun (id, vv, data) -> id)
  |> dict

let ensureDirectory s = 
  if not (Directory.Exists(s)) then Directory.CreateDirectory(s) |> ignore

let data = __SOURCE_DIRECTORY__ + "/data"

for c in ctx.Dbo.Code do
  if not (versions.ContainsKey(c.Id)) then
    printfn "Failed for id %d (%s)" c.Id (mangleId c.Id)
  else
    ensureDirectory(data + "/formatted-old/" + string c.Id) |> ignore
    ensureDirectory(data + "/source/" + string c.Id) |> ignore
    for _, version, (formatted, source) in versions.[c.Id] do
      File.WriteAllText(data + "/formatted-old/" + string c.Id + "/" + (string version), formatted)
      File.WriteAllText(data + "/source/" + string c.Id + "/" + (string version), source)

#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll"
open FSharp.Data

type Snippets = JsonProvider<"""{"snippets":
 [{ "id": 0, "title": "Sample", "comment": "Sample", "author": "Sample", "link": "Sample",
    "date": "2015-06-11T23:39:43.6454087-04:00", "likes": 3, "isPrivate": true, "passcode": "fsharp",
    "references": ["FSharp","Data"], "source": "fun3d", "versions": 3, "displayTags": ["fsharp", "sample"],
    "enteredTags": ["fsharp", "sample"] },
  { "id": 0, "title": "Sample", "comment": "Sample", "author": "Sample", "link": "Sample",
    "date": "2015-06-11T23:39:43.6454087-04:00", "likes": 3, "isPrivate": true, "passcode": "fsharp",
    "references": ["FSharp","Data"], "source": "fun3d", "versions": 3, "displayTags": ["fsharp", "sample"],
    "enteredTags": ["fsharp", "sample"] }] }""">

let buildIndex passFunc = 
  [| for i in ctx.Dbo.Code ->
      let refs = i.References.Split([|','|], StringSplitOptions.RemoveEmptyEntries) |> Array.map (fun s -> s.Trim())
      let tags = [| for t in i.``dbo.CodeTags by ID`` -> t.Tag |]
      Snippets.Snippet
        ( id = i.Id, title = i.Title, comment = i.Comment, author = i.Author, link = i.Link,
          date = i.Date, likes = i.Likes, isPrivate = i.Private, passcode = passFunc i.Passcode,
          references = refs, source = i.Source, versions = Seq.length i.``dbo.Versions by ID``,
          enteredTags = tags, displayTags = tags ) |]

let snippets = buildIndex sha1Hash
let snippetsNoPss = buildIndex (fun _ -> "")

File.WriteAllText(data + "/index.json", Snippets.Root(snippets).JsonValue.ToString())
File.WriteAllText(data + "/index-no-pass.json", Snippets.Root(snippetsNoPss).JsonValue.ToString())