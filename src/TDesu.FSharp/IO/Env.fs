namespace TDesu.FSharp.IO

open System

/// <summary>
/// Environment variable helpers.
/// </summary>
/// <example>
/// <code>
/// let connStr = Env.requireVar "DATABASE_URL"
/// let port = Env.getVarOr "8080" "PORT"
/// </code>
/// </example>
[<RequireQualifiedAccess>]
module Env =
    /// Gets an environment variable as Some, or None if missing/empty.
    /// <param name="name">The environment variable name.</param>
    let getVar (name: string) : string option =
        match Environment.GetEnvironmentVariable(name) with
        | null
        | "" -> None
        | v -> Some v

    /// Gets an environment variable, throwing if missing/empty.
    /// <param name="name">The environment variable name.</param>
    /// <exception cref="System.InvalidOperationException">When the variable is not set or empty.</exception>
    let requireVar (name: string) : string =
        match getVar name with
        | Some v -> v
        | None -> invalidOp $"Environment variable '{name}' is not set"

    /// Gets an environment variable, or a default if missing/empty.
    /// <param name="defaultValue">The fallback value.</param>
    /// <param name="name">The environment variable name.</param>
    let getVarOr (defaultValue: string) (name: string) : string =
        match getVar name with
        | Some v -> v
        | None -> defaultValue
