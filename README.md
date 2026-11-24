# WiktiView

An ASP.NET Core MVC (.NET 11) app for browsing and searching `wiktionary-de.json` (165k German words).

## Data file

`wiktionary-de.json` is not in the repository because of its size (~168 MB). After cloning, copy it into the repository root (next to `WiktiView.slnx`). To use a different location, change `Dictionary:DataFilePath` in `appsettings.json`.

## Running

```
cd src/WiktiView.Web
dotnet run --launch-profile http
```

Then open http://localhost:5074.

The data file path is set in `appsettings.json` → `Dictionary:DataFilePath`. Relative paths are resolved against the project folder.

## How it works

The 168 MB file is never loaded into memory as a whole.

1. **Indexing (one time):** `JsonRecordScanner` streams the file in 1 MB chunks using `Utf8JsonReader`. For each record it stores only the byte offset and length, plus a few searchable fields (lemma, word type, article, TR/EN translations). It does not depend on how the JSON is formatted. The index is written to `App_Data/wiktionary-de.idx`. On later starts it loads from that cache in under a second. If the data file's size or modification date changes, the index is rebuilt automatically.
2. **Search:** runs entirely on the in-memory index and never touches the data file. Matching ignores case and diacritics (`hauser` = `Häuser`, `strasse` = `Straße`).
3. **Display:** only the records on the requested page are read from disk by their byte offset, using `RandomAccess`.

While the index is being built the app keeps serving requests. Pages return a 503 progress page that refreshes itself.

## Features

- Search by German word, Turkish meaning, English translation, or all of them. Match modes are contains, starts with, and exact.
- Filters for word type, article (der/die/das), and "has a Turkish meaning".
- Sort orders: best match, alphabetical (German collation), or file order. Page size can be changed, and you can jump to a page directly.
- A–Z quick navigation.
- Details page with senses, examples, tags, conjugation table, imperative, noun and adjective forms, translations, synonyms and antonyms (clickable), IPA, audio, previous/next word, and the raw JSON.
- `GET /Words/Json/{id}` returns the raw record.

## Structure

```
src/WiktiView.Web
├── Configuration/   DictionaryOptions (appsettings binding + validation)
├── Domain/          WordEntry (record model), metadata, JSON settings
├── Indexing/        Streaming scanner, index builder, binary cache, index state
├── Hosting/         Background indexing service, filter that waits for the index
├── Search/          Search criteria, normalizer, search service, paging
├── Storage/         WordReader: reads a single record by offset
├── Presentation/    Turkish labels
├── Controllers/     Home, Words
└── Views/           Bootstrap classes only, no custom CSS/JS
```

Data: Wiktionary (de.wiktionary.org), CC BY-SA 4.0.
