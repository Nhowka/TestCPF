namespace Shared

open System

module Helpers =
    let private zero = int '0'
    let (|Numbers|) (s: string) = s |> String.filter Char.IsDigit

    let (|AsDigits|) (Numbers s) =
        s |> Seq.map (fun c -> int c - zero) |> Seq.toList

    let (|Mod11|) x = x % 11

open Helpers



type CPF =
    | Incomplete of string
    | Valid of string
    | Invalid of string

    static member Parse(Numbers numbers) =
        if numbers.Length > 11 then
            Invalid numbers
        elif numbers.Length < 11 then
            Incomplete numbers
        else
            let verify (AsDigits digits) =
                let formatted =
                    let pattern = "000.000.000-00" |> Seq.toList
                    // Replaces the '0' character with the CPF digit
                    let rec inner acc pattern digits =
                        match (pattern, digits) with
                        | [], _
                        | _, [] -> acc |> List.rev |> String.concat ""
                        | '0' :: ps, d :: ds -> inner (string d :: acc) ps ds
                        | p :: ps, ds -> inner (string p :: acc) ps ds

                    inner [] pattern digits

                // Rule for using 0 when 11 minus the remainder the remainder is 10 or 11
                let dv (Mod11 n) = if n < 2 then 0 else 11 - n

                // Keep the constant for multiplying and the sum of the digits
                // Prevents iterating the list of digits twice
                let rec inner (k1, k2) (s1, s2) digits =
                    match digits with
                    // This case won't happen as only cases with 11 digits reach this function
                    | [] -> Invalid numbers
                    | d :: ds ->
                        // Time to check the first digit
                        if k1 = 1 then
                            // First digit different than expected
                            if dv s1 <> d then
                                Invalid numbers
                            else
                                inner (0, k2 - 1) (s1 + d * k1, s2 + d * k2) ds
                        // Time to check the second digit
                        elif k2 = 1 then
                            if dv s2 <> d then
                                Invalid numbers
                            else
                                // Second digit is the expected, return the formatted cpf
                                Valid formatted
                        else
                            // Common case, decreases the constant and sum the digits multiplied by the expected constant
                            inner (k1 - 1, k2 - 1) (s1 + k1 * d, s2 + k2 * d) ds

                // Check that digits aren't the same
                if digits
                   |> Seq.pairwise
                   |> Seq.forall (fun (a, b) -> a = b) then
                    Invalid numbers
                else
                    inner (10, 11) (0, 0) digits

            verify numbers

module CPF =
    type Request = { cpf: string }
    type Response = { score: int; created_at: DateTime }
    type ResponseError = {error:string}
