module Server

open Saturn

open Shared
open Shared.CPF
open System.Threading.Tasks
open FSharp.Control.Tasks
open NpgsqlTypes


module Database =
    open System.Reflection
    open DbUp
    open System
    open Npgsql

    [<Literal>]
    let DB =
        "User ID=postgres;Password=postgres;Host=localhost;Port=5432;Database=postgres"

    let getDatabaseConnection () =
        let conn = new NpgsqlConnection(DB)
        conn.Open()
        conn


    let processAll (f: 'a -> Task<'b>) (list: 'a list) : 'b list Task =
        let t =
            list
            |> List.rev
            |> List.fold
                (fun ctx next ->
                    fun () ->
                        task {
                            let! l = ctx ()
                            let! n = f next
                            return n :: l
                        })
                (fun _ -> Task.FromResult [])

        t ()


    let recoverCPF conn cpf : CPF.Response option Task =
        task {

            use comm =
                new NpgsqlCommand("SELECT score, created_at FROM cpfs where cpf = sha512(@cpf::bytea)", conn)

            comm.Parameters.AddWithValue("@cpf", NpgsqlDbType.Text, cpf)
            |> ignore<NpgsqlParameter>

            use! query = comm.ExecuteReaderAsync()
            let! hasRows = query.ReadAsync()

            if hasRows then
                return
                    { score = query.GetInt32(0)
                      created_at = query.GetDateTime(1) }
                    |> Some
            else
                return None
        }

    let addCPF conn cpf score =
        task {
            use comm =
                new NpgsqlCommand(
                    """INSERT INTO cpfs (cpf, score) values (sha512(@cpf::bytea), @score)
                ON CONFLICT(cpf) DO UPDATE SET score=cpfs.score
                RETURNING cpfs.score, cpfs.created_at """,
                    conn
                )

            comm.Parameters.AddWithValue("@cpf", NpgsqlDbType.Text, cpf)
            |> ignore<NpgsqlParameter>

            comm.Parameters.AddWithValue("@score", NpgsqlDbType.Integer, score)
            |> ignore<NpgsqlParameter>

            use! query = comm.ExecuteReaderAsync()
            let! _ = query.ReadAsync()

            return
                { score = query.GetInt32(0)
                  created_at = query.GetDateTime(1) }
        }

    do
        try
            let result =
                DeployChanges
                    .To
                    .PostgresqlDatabase(DB)
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                    .LogToConsole()
                    .Build()
                    .PerformUpgrade()

            if result.Successful then
                eprintfn "Migration was ok"
            else
                eprintfn "%s: %s" (result.Error.GetType().Name.ToUpper()) result.Error.Message
        with
        | _ -> eprintfn "Unexpected error"


open Giraffe


let addCPF: string -> HttpHandler =
    let randomScore =
        let r = System.Random()
        fun () -> 1 + r.Next(1000)

    fun cpf ->
        match CPF.Parse cpf with
        | Invalid cpf
        | Incomplete cpf ->
            setStatusCode 400
            >=> json { error = $"Invalid CPF: {cpf}" }
        | Valid cpf ->
            fun next ctx ->
                task {
                    use conn = Database.getDatabaseConnection ()
                    let score = randomScore ()
                    let! inserted = Database.addCPF conn cpf score

                    return! json inserted next ctx
                }

let recoverCPF cpf =

    printfn "yay"

    match CPF.Parse cpf with
    | Invalid cpf
    | Incomplete cpf ->
        setStatusCode 400
        >=> json { error = $"Invalid CPF: {cpf}" }
    | Valid cpf ->
        fun next ctx ->
            task {
                use conn = Database.getDatabaseConnection ()

                match! Database.recoverCPF conn cpf with
                | None -> return! RequestErrors.notFound (json {error= $"CPF {cpf} is not on database"}) next ctx
                | Some recovered -> return! json recovered next ctx
            }


let webApp =
    choose [ POST
             >=> route "/score"
             >=> bindJson (fun { cpf = cpf } -> addCPF cpf)
             GET >=> routef "/score/%s" recoverCPF ]



let app =
    application {
        url "http://0.0.0.0:8085"
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
    }

run app
