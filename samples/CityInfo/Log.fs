module Log

open System

/// Minimal ANSI-colored logger. No DI, no ILogger — just printfn.
/// Demonstrates: UnixTime.seconds, ^ operator, % operator.

let private formatTime () =
    let now = DateTimeOffset.UtcNow
    now.ToString("HH:mm:ss")

let private write (color: string) (level: string) (msg: string) =
    let ts = formatTime ()
    printfn $"\x1b[90m[%s{ts}\x1b[0m %s{color}%s{level}\x1b[0m\x1b[90m]\x1b[0m %s{msg}"

let info msg = write "\x1b[32m" "INF" msg
let warn msg = write "\x1b[33m" "WRN" msg
let error msg = write "\x1b[31m" "ERR" msg
let debug msg = write "\x1b[36m" "DBG" msg

let infof fmt = Printf.kprintf info fmt
let warnf fmt = Printf.kprintf warn fmt
let errorf fmt = Printf.kprintf error fmt
let debugf fmt = Printf.kprintf debug fmt
