module Shared.Tests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Shared

let shared = testList "Shared" [
    testCase "Less than 11 digits in CPF is invalid" <| fun _ ->
        let testString = "123456789"
        let expected = Incomplete testString
        let actual = CPF.Parse testString
        Expect.equal actual expected "Should be equal"
    testCase "Valid CPF is formatted" <| fun _ ->
        let testString = "12345678909"
        let expected = Valid "123.456.789-09"
        let actual = CPF.Parse testString
        Expect.equal actual expected "Should be equal"
    testCase "Invalid CPF is not formatted" <| fun _ ->
        let testString = "12345678919"
        let expected = Invalid testString
        let actual = CPF.Parse testString
        Expect.equal actual expected "Should be equal"
    testCase "More than 11 digits is invalid" <| fun _ ->
        let testString = "123456789091"
        let expected = Invalid testString
        let actual = CPF.Parse testString
        Expect.equal actual expected "Should be equal"
    testCase "Same 11 digits is invalid" <| fun _ ->
        let testString = "11111111111"
        let expected = Invalid testString
        let actual = CPF.Parse testString
        Expect.equal actual expected "Should be equal"

]