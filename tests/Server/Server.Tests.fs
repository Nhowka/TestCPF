module Server.Tests

open Expecto

open Shared
open Server
open Npgsql
open ThrowawayDb.Postgres
open DbUp

open FSharp.Control.Tasks

let inline useDatabaseConnection connFunc =
    use db =
        ThrowawayDatabase.Create(username = "postgres", password = "postgres", host = "localhost")

    DeployChanges
        .To
        .PostgresqlDatabase(db.ConnectionString)
        .WithScriptsEmbeddedInAssembly(System.Reflection.Assembly.GetExecutingAssembly())
        .LogToConsole()
        .Build()
        .PerformUpgrade()
    |> ignore

    use conn =
        new NpgsqlConnection(db.ConnectionString)

    conn.Open()
    let result = connFunc conn
    conn.Close()
    result

let server =
    testList
        "Server"
        [ testCase "CPF not inserted returns None"
          <| fun _ ->
              match CPF.Parse "12345678909" with
              | Valid cpf ->
                  useDatabaseConnection
                      (fun conn ->
                          task {
                              let! recoveredScore = Database.recoverCPF conn cpf

                              Expect.isNone recoveredScore "Score should be None"
                          }
                          |> Async.AwaitTask
                          |> Async.RunSynchronously

                          )
              | cpf -> Expect.isTrue false $"CPF {cpf} should be valid"
          testCase "Adding valid CPF"
          <| fun _ ->
              match CPF.Parse "12345678909" with
              | Valid cpf ->
                  useDatabaseConnection
                      (fun conn ->
                          task {
                              let score = System.Random().Next(1000)
                              let! addedScore = Database.addCPF conn cpf score
                              let! recoveredScore = Database.recoverCPF conn cpf

                              Expect.equal (Some addedScore) recoveredScore "Scores should be equal"

                          }
                          |> Async.AwaitTask
                          |> Async.RunSynchronously

                          )
              | cpf -> Expect.isTrue false $"CPF {cpf} should be valid"




          ]

let all =
    testList "All" [ Shared.Tests.shared; server ]

[<EntryPoint>]
let main _ = runTestsWithCLIArgs [] [||] all
