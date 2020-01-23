![alt tag](https://github.com/jchristn/indexengine/blob/master/assets/icon.png)

# IndexEngine

[![][nuget-img]][nuget]

[nuget]:     https://www.nuget.org/packages/IndexEngine
[nuget-img]: https://badge.fury.io/nu/Object.svg

IndexEngine is a simple indexer written in C# using Sqlite as a storage repository.  As of release 1.0.5, IndexEngine is targeted to both .NET Core 2.0 and .NET Framework 4.5.2.

## New in v1.0.10

- Fixes to database INSERT/SELECT and string case (thanks @teub!)
- Fix for divide by zero problem (thanks @teub!)

## Help or feedback

First things first - do you need help or have feedback?  Contact me at joel dot christner at gmail dot com or file an issue here!

## Performance and scale

It's pretty quick :)  It hasn't been tested with huge document libraries or anything, so I'd recommend testing thoroughly before using in production. 

## Simple Example
```
using Indexer;

IndexEngine ie = new IndexEngine("idx.db");

// Add a document
Document d = new Document(
  "Title",             // i.e. Mark Twain
  "Description",       // i.e. A Great Book
  "File Path or URL",  // i.e. C:\Documents\MarkTwain.txt
  "Source",            // i.e. The Internet
  "AddedBy",           // i.e. Joel
  Encoding.UTF8.GetBytes("This is some sample data for indexing")
);
ie.SubmitDocument(d);       // async, returns immediately
ie.SubmitDocumentSync(d);   // sync, returns after completion

// Search the index
List<string> terms = new List<string> { "some", "data" };
List<Document> results = ie.SearchIndex(terms, null);
foreach (Document d in results) Console.WriteLine(d.ToString());

// List number of threads actively indexing documents
Console.WriteLine("Number of documents being indexed right now: " + ie.GetProcessingThreadsCount());

// Get the names of docs that are currently being indexed
List<string> activeDocs = ie.GetProcessingDocumentsList();

// Delete documents from the index
ie.DeleteDocumentByGuid("abcd1234...");
ie.DeleteDocumentByHandle("C:\\Documents\\MarkTwain.txt");

// Retrieve documents
Document d = RetrieveDocumentByGuid("abcd1234...");
Document d = RetrieveDocumentByHandle("C:\\Documents\\MarkTwain.txt");
bool b = IsHandleIndexed("C:\\Documents\\MarkTwain.txt");
```
