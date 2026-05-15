# Conway's Game of Life API

This project is a .NET 9 Web API for Conway's Game of Life. It lets you upload a board, retrieve the next generation, retrieve a board many generations ahead, and ask for the final stable state.

## Running The API

From the repo root, run:

```bash
dotnet run --project GameOfLife.Api
```

In Development, Swagger UI is available at:

```text
/swagger
```

The exact localhost URL is shown in the terminal after running the app.

## Uploading A Board

Use `POST /boards`.

The easiest request format is `rows`. Use `#` for a live cell and `.` for a dead cell:

```json
{
  "rows": [
    ".....",
    "..#..",
    "..#..",
    "..#..",
    "....."
  ]
}
```

The API also accepts a boolean matrix:

```json
{
  "cells": [
    [false, false, false],
    [false, true, false],
    [false, false, false]
  ]
}
```

Send either `rows` or `cells`. The response contains the board id:

```json
{
  "id": "00000000-0000-0000-0000-000000000000"
}
```

## Endpoints

`GET /boards/{id}` returns the original uploaded board.

`GET /boards/{id}/next` returns the next generation.

`GET /boards/{id}/states/{generations}` returns the board after the requested number of generations.

`GET /boards/{id}/final?maxAttempts=1000` returns the final stable state. If the board does not become stable within `maxAttempts`, the API returns an error.

## Response Format

Board responses include both formats:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "generation": 1,
  "cells": [
    [false, false, false],
    [true, true, true],
    [false, false, false]
  ],
  "isStable": false,
  "rows": [
    "...",
    "###",
    "..."
  ]
}
```

Use `rows` when testing in Swagger because it is easier to read.

## Persistence

Uploaded boards are stored in `GameOfLife.Api/Data/boards.json`. This means the service can restart and still retain uploaded boards.

The `Data` folder is ignored by git so local board data is not pushed.

## Validation Rules

Boards must contain at least one row and one cell. All rows must be the same length. In `rows` format, only `#` and `.` are valid characters.

## Testing

Run:

```bash
dotnet test
```

The tests cover the Game of Life rules, persistence, API upload, next-state behavior, readable `rows` responses, and final-state error behavior.
