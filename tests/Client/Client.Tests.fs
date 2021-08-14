module Client.Tests

open Fable.Mocha

open Index
open Shared

let client = testList "Client" [
    testCase "Input works" <| fun _ ->
        let model, _ = init ()

        let model, _ = update (SetInput "test") model

        Expect.equal "test" model.Input "Input should equal test"
]

let all =
    testList "All"
        [
#if FABLE_COMPILER // This preprocessor directive makes editor happy
            Shared.Tests.shared
#endif
            client
        ]

[<EntryPoint>]
let main _ = Mocha.runTests all