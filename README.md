F# Snippets - Tools
===================

Various scripts and other tools for working with the F# snippets data.

 * `azure.fsx` initializes Azure storage and uploads data to blobs
 * `format.fsx` formats all snippets as HTML from the source code
 * `load.fsx` reads data from the old fssnip SQL database
 
To run formatting or the Azure script, you can download data from 
[the data dump repository](https://github.com/fssnippets/fssnip-tools) and put 
it all into a `data` subfolder. In `azure.fsx`, you will also need to enter
your connection string for Azure storage. The `load.fsx` script is not much use
if you don't have the original data dumps, but is kept here just in case...
