#load "packages/FSharp.Azure.StorageTypeProvider/StorageTypeProvider.fsx"
#load "config.fsx"
open FSharp.Azure.StorageTypeProvider
open System.IO

type Azure = AzureTypeProvider<Config.azureConnectionString>

let target = Path.Combine(__SOURCE_DIRECTORY__, "data")
let op = Azure.Containers.data.Download(target)
Async.RunSynchronously(op)
