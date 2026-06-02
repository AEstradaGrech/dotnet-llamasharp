# dotnet-llamasharp

This repo was originally a chatbot I was making for another personal project, then it became the sample project for the Estrada.ChromaDB.Repositories, then the sample project for the Estrada.OllamaSharp.LameChain.SDK
and now it is a sample project and also a bolierplate project to develop Rest API's for chatbot and RAG applications with ollama using OllamaSharp and LameChain, so it case you are building a Generative AI application
with .NET and you are comfortable with a Commands based architecture, this could be a good starting point (just review the examples to get familiar with the framework then delete them).

Because it is mostly a sample project to show a few implementation examples of my ChromaDB and LameChain nuget packages, the API includes some basic services of chatbot applictions using chroma as memory for both
chat and RAG applications using LameChain Commands to implement the LLM based processes and orchestrating the results 'manually', or the same processes but using the fluent extensions of LameChain SDK.

# What will you find in the repository:

An .Net 10 microservice following a DDD (lite) architecture with three layers:

- API & Service layer: implements all domain services and handles the DTO-DomainModle mappings.
- Domain: contains all the domain models and contracts, including service and repository interfaces.
- Infrastructure: handles the DB access. Here you will find the ChromaRepository implementations.

Scattered through those app layers, usage examples for this nugets:

https://www.nuget.org/packages/Estrada.OllamaSharp.LameChain.SDK

https://www.nuget.org/packages/Estrada.ChromaDB.Repositories

https://www.nuget.org/packages/Estrada.LangSearch.SDK

## If you are looking for implementation examples of the ChromaDB package, here you will find more 'real-case' scenario of how can you extend the framework classes to adapt them to your application needs / domain models.

The API makes use of the ChromaDB repositories nuget to store Files to use them as a data source for RAG apps, and Chats to use them as a memory for a chatbot thak works by input similarity to retrieve the chat history. Also,
the API uses Chroma as simple text database to store SystemMessages for your LLM requests.

### More details about the Chroma DB examples:

If you are comming from the Estrada.ChromaDB.Repositories README and you are looking for real 'how to use' examples, you might want to review this, for example:

	- ChromaFilesService.cs: this is an implementation of a ChromaCollection intended to be used as a data source for a RAG chatbot. You can see how to extend the Metadata classes to add different 'table' fields for Collections and Chunks independently
							 and how to work with the SDK
	- ChromaChatsRepository: here you can see a good example of how to extend a repository and adapt it to your domain logic, and how you can use a simple tagging system to sub-group chunks in ChatSessions.
	- ChromaService.cs: here is an exmple of how you can use the 'DefaultImplementation', to review any kind of custom collection in general terms (review the text, validate your embedding process...)
	- ChromaSysChunksRepository.cs: the only thing to note with this (appart of another implementation example) is that you can use the Chroma Repos as a simple documental database (in the style of MongoDB) as a support for your Generative AI apps. In this case is used
						   to store long System Messages for different LLM tasks and work comfortably with different versions (very useful to create 'checkpoints' of your instruction prompts during development)

## If you are looking for implementation examples of the LameChain SDK nuget, here you will find a few examples of how to use the package classes to instantiate and use different type of Lame Commands as 'standalone' pieces of the process or chained with the Fluent API

The project includes a 'Samples' folder to demonstrate how to use the SDK in different cases. You can check the 'LameSamplesController' to see a general showcase with a few examples, from the most basic command usages, to full fluent chains with more complex setup.
There are also good fuctional usage examples in the PromptsController. For example, you can review the '/smart/qa/query' to see simple example of RAG application that makes use of Lame Commands to orchestrate the process logic using the LLM as an evaluator and 
selector of data sources, and then you can see the same process 'translated' to fluent LameChain in the '/smart/chain/qa' endpoint.

There is also a 'conceptual' example in the LameSamplesService that is not meant to be executed but shows more in detail how to build chains from very basic sequences to more complex chains with parallel steps or conditional branches (consider it a 'quickstart' section).

### More details about the LameChaine examples:

If you are comming from the Estrada.OllamaSharp.LameChain.SDK README and you are looking for implementation examples to use the framework, then review all the content of any 'Samples' folder in the project and the above mentioned examples. You also might
want to review this:
	
	- LameSamplesController.cs: here you can see how you can use the CommandsService to work easily with the Core Commands instead of using the Factory (which offers a less tight interface). You can extend the class to create a custom service that provides easy
							    and clean access to the most used commands of your application, for example.
							    In this class you will find also examples / test-case endpoints for the Atomic Value Commands that you may find useful as basic commands usage examples.
	- RagService.cs: Here you can see the mentioned rag examples that make the same process with single Lame Commands and with Fluent LameChain. These are good examples of basic 'real-case' scenarios for both the LameChain and the ChromaDB.Repos packages.
	- OllamaSharpService.cs: a really simple implementation example of prompting the LLM using Lame Commands.

# Where are the tests?

Incoming (I will add them at some point but, being a personal project it is in the TODO list... always the last one)