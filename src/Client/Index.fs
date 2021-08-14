module Index

open Elmish
open Shared
open Shared.CPF
open Browser
open Thoth.Json
open Thoth.Fetch

type Model =
    { Input: string
      Result: Result<Response, ResponseError> option }

module Decoders =

    let responseErrorDecoder =
        Decode.object (fun get -> { error = get.Required.Field "error" Decode.string })

    let responseDecoder =
        Decode.object
            (fun get ->
                { score = get.Required.Field "score" Decode.int
                  created_at = get.Required.Field "created_at" Decode.datetime })

type Msg =
    | SetInput of string
    | AddCPF
    | RequestCPF
    | SetResult of Result<Response, ResponseError>


let requestError =
    { error = "Request had problems, try again later" }

let recoverCPF cpf =
    promise {
        match! Fetch.tryGet ($"/score/{cpf}", decoder = Decoders.responseDecoder) with
        | Error (FetchFailed response) ->
            let! error = response.text ()

            return
                error
                |> Decode.unsafeFromString (Decoders.responseErrorDecoder)
                |> Error
        | Error _ -> return Error requestError
        | Ok response -> return Ok response
    }

let addCPF cpf =
    promise {
        match! Fetch.tryPost ($"/score", Encode.object [ "cpf", cpf ], decoder = Decoders.responseDecoder) with
        | Error (FetchFailed response) ->
            let! error = response.text ()

            return
                error
                |> Decode.unsafeFromString (Decoders.responseErrorDecoder)
                |> Error
        | Error _ -> return Error requestError
        | Ok response -> return Ok response
    }

let init () : Model * Cmd<Msg> = { Input = ""; Result = None }, Cmd.none

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | SetInput value -> { model with Input = value }, Cmd.none
    | AddCPF ->
        let cmd =
            Cmd.OfPromise.perform addCPF model.Input SetResult

        { model with Result = None }, cmd
    | RequestCPF ->
        let cmd =
            Cmd.OfPromise.perform recoverCPF model.Input SetResult

        { model with Result = None }, cmd
    | SetResult result -> { model with Result = Some result }, Cmd.none

open Feliz
open Feliz.Bulma

let navBrand =
    Bulma.navbarBrand.div [
        Bulma.navbarItem.a [
            prop.href "https://safe-stack.github.io/"
            navbarItem.isActive
            prop.children [
                Html.img [
                    prop.src "/favicon.png"
                    prop.alt "Logo"
                ]
            ]
        ]
    ]

let containerBox (model: Model) (dispatch: Msg -> unit) =
    let (disabled, fieldColor, text) =
        match CPF.Parse model.Input with
        | Incomplete cpf -> true, color.isInfo, cpf
        | Invalid cpf -> true, color.isDanger, cpf
        | Valid cpf -> false, color.isSuccess, cpf

    Bulma.box [
        Bulma.content [
            Html.ol [
                match model.Result with
                | Some (Ok { score = score; created_at = created }) ->
                    Html.ol [ prop.text $"Score: {score}" ]

                    Html.ol [
                        prop.text $"Created at: {created.ToShortDateString()} {created.ToShortTimeString()}"
                    ]
                | Some (Error { error = error }) -> Html.ol [ prop.text $"Error: {error}" ]
                | None -> ()
            ]
        ]
        Bulma.field.div [
            field.isGrouped
            prop.children [
                Bulma.control.p [
                    control.isExpanded
                    prop.children [
                        Bulma.input.text [
                            prop.value text
                            fieldColor
                            prop.placeholder "CPF to query"
                            prop.onChange (SetInput >> dispatch)
                        ]
                    ]
                ]

                Bulma.control.p [
                    Bulma.button.a [
                        fieldColor
                        prop.disabled disabled
                        prop.onClick (fun _ -> if not disabled then dispatch AddCPF)
                        prop.text "Add"
                    ]
                ]
                Bulma.control.p [
                    Bulma.button.a [
                        fieldColor
                        prop.disabled disabled
                        prop.onClick (fun _ -> if not disabled then dispatch RequestCPF)
                        prop.text "Request"
                    ]
                ]
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Bulma.hero [
        hero.isFullHeight
        color.isPrimary
        prop.style [
            style.backgroundSize "cover"
            style.backgroundImageUrl "https://unsplash.it/1200/900?random"
            style.backgroundPosition "no-repeat center center fixed"
        ]
        prop.children [
            Bulma.heroHead [
                Bulma.navbar [
                    Bulma.container [ navBrand ]
                ]
            ]
            Bulma.heroBody [
                Bulma.container [
                    Bulma.column [
                        column.is6
                        column.isOffset3
                        prop.children [
                            Bulma.title [
                                text.hasTextCentered
                                prop.text "DR"
                            ]
                            containerBox model dispatch
                        ]
                    ]
                ]
            ]
        ]
    ]
