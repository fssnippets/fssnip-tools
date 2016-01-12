// -------------------------------------------------------------------------------------------------
// Reading F# snippets from database used by the old version
// -------------------------------------------------------------------------------------------------

#r "packages/SQLProvider/lib/net40/FSharp.Data.SqlProvider.dll"
open System
open System.IO
open FSharp.Data.Sql

// ------------------------------------------------------------------------------------------------

let alphabet = [ '0' .. '9' ] @ [ 'a' .. 'z' ] @ [ 'A' .. 'Z' ] |> Array.ofList
let alphabetMap = Seq.zip alphabet [ 0 .. alphabet.Length - 1 ] |> dict

/// Generate mangled name to be used as part of the URL
let mangleId i =
  let rec mangle acc = function
    | 0 -> new String(acc |> Array.ofList)
    | n -> let d, r = Math.DivRem(n, alphabet.Length)
           mangle (alphabet.[r]::acc) d
  mangle [] i

/// Translate mangled URL name to a numeric snippet ID
let demangleId (str:string) = 
  let rec demangle acc = function
    | [] -> acc
    | x::xs -> 
      let v = alphabetMap.[x]
      demangle (acc * alphabet.Length + v) xs
  demangle 0 (str |> List.ofSeq)

// ------------------------------------------------------------------------------------------------

type FsSnip = 
  SqlDataProvider<"Data Source=.\\SQLExpress;Initial catalog=fssnip_data;Integrated security=true",
                  DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER>

let ctx = FsSnip.GetDataContext()

let versions = 
  [ for v in ctx.``[dbo].[Versions]`` -> 
      v.CodeID, v.VersionNumber, (v.Formatted, v.Source) ]
  |> Seq.groupBy (fun (id, vv, data) -> id)
  |> dict

let EnsureDirectory s = 
  if not (Directory.Exists(s)) then Directory.CreateDirectory(s) |> ignore

let data = __SOURCE_DIRECTORY__ + "/data"

for c in ctx.``[dbo].[Code]`` do
  if not (versions.ContainsKey(c.ID)) then
    printfn "Failed for id %d (%s)" c.ID (mangleId c.ID)
  else
    EnsureDirectory(data + "/formatted/" + string c.ID) |> ignore
    EnsureDirectory(data + "/source/" + string c.ID) |> ignore
    for _, version, (formatted, source) in versions.[c.ID] do
      File.WriteAllText(data + "/formatted/" + string c.ID + "/" + (string version), formatted)
      File.WriteAllText(data + "/source/" + string c.ID + "/" + (string version), source)

#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll"
open FSharp.Data

type Snippets = JsonProvider<"""{"snippets":
 [{ "id": 0, "title": "Sample", "comment": "Sample", "author": "Sample", "link": "Sample",
    "date": "2015-06-11T23:39:43.6454087-04:00", "likes": 3, "isPrivate": true, "passcode": "fsharp",
    "references": ["FSharp","Data"], "source": "fun3d", "versions": 3, "tags": ["fsharp", "sample"],
    "enteredTags": ["fsharp", "sample"] },
  { "id": 0, "title": "Sample", "comment": "Sample", "author": "Sample", "link": "Sample",
    "date": "2015-06-11T23:39:43.6454087-04:00", "likes": 3, "isPrivate": true, "passcode": "fsharp",
    "references": ["FSharp","Data"], "source": "fun3d", "versions": 3, "tags": ["fsharp", "sample"],
    "enteredTags": ["fsharp", "sample"] }] }""">

let snippets = 
  [| for i in ctx.``[dbo].[Code]`` ->
      let refs = i.References.Split([|','|], StringSplitOptions.RemoveEmptyEntries) |> Array.map (fun s -> s.Trim())
      let tags = [| for t in i.FK_CodeTags_Code -> t.Tag |]
      Snippets.Snippet
        ( id = i.ID, title = i.Title, comment = i.Comment, author = i.Author, link = i.Link,
          date = i.Date, likes = i.Likes, isPrivate = i.Private, passcode = "", // i.Passcode,
          references = refs, source = i.Source, versions = Seq.length i.FK_Versions_Code,
          tags = tags, enteredTags = tags ) |]

File.WriteAllText(data + "/index.json", Snippets.Root(snippets).JsonValue.ToString())


