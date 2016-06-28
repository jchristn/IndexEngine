# index engine
simple indexer in C#

Please refer to the test console application in the solution for testing the library. 

## help or feedback
first things first - do you need help or have feedback?  Contact me at joel at maraudersoftware.com dot com or file an issue here!

## description
index engine is a simple indexer written in C# using Sqlite as a storage repository.  Mono.Data.Sqlite was chosen specifically to ensure compatibility with Mono.

## performance and scale
it's pretty quick :)  It hasn't been tested with huge document libraries or anything, so I'd recommend testing thoroughly before using in production. 

## sample app
```
using Indexer;
```

```
IndexEngine ie = new IndexEngine("idx.sqlite");
```

Indexing a document:
```
Document d = new Document(
  "Title",				// i.e. Mark Twain
  "Description",		// i.e. A Great Book
  "File Path or URL",	// i.e. C:\Documents\MarkTwain.txt
  "Source",				// i.e. The Internet
  "AddedBy",			// i.e. Joel
  Encoding.UTF8.GetBytes("This is some sample data for indexing")
);
ie.SubmitDocument(d);
```

Searching the index:
```
List<string> terms = new List<string> { "some", "data" };
List<Document> results = ie.SearchIndex(terms, null);
foreach (Document d in results) Console.WriteLine(d.ToString());
```

Various other APIs:
```
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
