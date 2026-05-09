#!/usr/bin/env -S dotnet fsi

open System.IO
open System

// Formats the matrix edges into an adjacency list string
// e.g. [(1, [2, 3]), (2, [1, 5])]
let getAdjacencyList (linewords: string array array) =
    linewords
    // Parse MTX edges. Keeping 1-based index as shown in your example.
    // Assuming undirected graph: generate both (u, v) and (v, u)
    |> Array.collect (fun x ->
        let u = int x.[0]
        let v = int x.[1]

        if u = v then
            [| (u, v) |] // Handle self-loops without duplicating
        else
            [| (u, v); (v, u) |])
    // Group by the source vertex
    |> Array.groupBy fst
    // Sort vertices just to make the output predictable and neat
    |> Array.sortBy fst
    |> Array.map (fun (u, edges) ->
        // Extract destinations, remove duplicates, and sort
        let neighbors = edges |> Array.map snd |> Array.distinct |> Array.sort

        let neighborsStr = neighbors |> Array.map string |> String.concat ", "

        sprintf "(%d, [%s])" u neighborsStr)
    |> String.concat ", "
    |> sprintf "[%s]"

// Generates the Inpla file content for BFS
let getExperimentBfsAdj graphStr =
    sprintf
        @"use ""./src/bfs.in"";

graph ~ %s;
const StartVertex = 1;
bfs(r) ~ (graph, StartVertex);
r; free ifce;
"
        graphStr

let usage = "Usage: dotnet fsi mtx_to_adj_experiment.fsx path/to/matrix.mtx"

let handleFile path =
    let lines = File.ReadLines(path) |> Seq.toArray

    // MTX format ignores comments starting with '%'
    let removedComments = lines |> Array.skipWhile (fun s -> s.StartsWith("%"))

    // Split lines into tokens, removing empty entries to avoid parse issues
    let linewords =
        removedComments
        |> Array.map (fun s -> s.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries))
        |> Array.filter (fun arr -> arr.Length >= 2)

    // First line after comments is nrows, ncols, nnz
    let first = linewords.[0]
    let nrows = int first.[0]
    let nnz = int first.[2]

    // Edges start from the second line
    let edges = Array.skip 1 linewords

    let experimentsDir = "./experiments_adj_bfs/"
    Directory.CreateDirectory(experimentsDir) |> ignore

    let newFilePath =
        Path.Combine(experimentsDir, Path.GetFileNameWithoutExtension path + ".in")

    let graphStr = getAdjacencyList edges
    let experiment = getExperimentBfsAdj graphStr

    File.WriteAllText(newFilePath, experiment)
    printfn "Written to %s. Processed %d vertices and %d non-zero records" newFilePath nrows nnz

let main (args: string array) =
    if Array.length args < 2 then
        eprintfn "%s" usage
        1
    else
        let path = args.[1]

        if not (File.Exists path) then
            eprintfn "File not found: %s" path
            1
        else
            handleFile path
            0

exit <| main fsi.CommandLineArgs
