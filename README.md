[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0%2B-blue.svg)](https://dotnet.microsoft.com/)

# 🦙 dotnet-llamasharp

This repo was originally a chatbot I was making for another personal project, then it became the sample project for the **Estrada.ChromaDB.Repositories**, then the sample project for the **Estrada.OllamaSharp.LameChain.SDK**, and now it is a sample project and also a boilerplate to develop REST APIs for chatbot and RAG applications with Ollama using OllamaSharp and LameChain.

If you're building a **Generative AI application** with .NET and you're comfortable with a Commands-based architecture, this could be a good starting point. (Just review the examples to get familiar with the framework, then delete them.)

Being mostly a sample project to showcase implementation examples of my ChromaDB and LameChain NuGet packages, the API includes basic services for chatbot applications using Chroma as memory for both chat and RAG applications using LameChain Commands to implement LLM-based processes, orchestrating results manually or using the fluent extensions of LameChain SDK.

---

## 📦 What You'll Find in This Repository

An **.NET 10 microservice** following a **DDD (lite)** architecture with three layers:

- **API & Service Layer**: Implements all domain services and handles DTO-DomainModel mappings
- **Domain**: Contains all domain models and contracts, including service and repository interfaces
- **Infrastructure**: Handles DB access, including ChromaRepository implementations

Throughout these layers, you'll find usage examples for these NuGet packages:

- 🔗 [**Estrada.OllamaSharp.LameChain.SDK**](https://www.nuget.org/packages/Estrada.OllamaSharp.LameChain.SDK)
- 🔗 [**Estrada.ChromaDB.Repositories**](https://www.nuget.org/packages/Estrada.ChromaDB.Repositories)
- 🔗 [**Estrada.LangSearch.SDK**](https://www.nuget.org/packages/Estrada.LangSearch.SDK)

---

## 🗄️ ChromaDB Repository Examples

> Looking for real-world scenarios on how to extend the framework classes to adapt them to your application needs and domain models?

The API uses the **ChromaDB repositories** NuGet to store:
- **Files** as data sources for RAG applications
- **Chats** as memory for chatbots that retrieve chat history by input similarity
- **System Messages** as simple text database entries for LLM requests

### Key Implementation Examples:

| File | Purpose |
|------|---------|
| **`ChromaFilesService.cs`** | Implementation of a ChromaCollection for RAG chatbot data sources. Shows how to extend Metadata classes and work with the SDK. |
| **`ChromaChatsRepository.cs`** | Example of extending a repository for domain logic and implementing a tagging system for ChatSessions. |
| **`ChromaService.cs`** | Demonstrates using the DefaultImplementation to review custom collections and validate your embedding process. |
| **`ChromaSysChunksRepository.cs`** | Shows how to use Chroma as a simple document database (MongoDB-style) to store long System Messages for different LLM tasks with version checkpoints. |

---

## 🔗 LameChain SDK Examples

> Looking for implementation examples of how to use package classes to instantiate and chain different types of Lame Commands?

The project includes a **`Samples`** folder demonstrating various use cases. Check the **`LameSamplesController`** for a general showcase ranging from basic command usage to complex fluent chains.

### Functional Examples in the PromptsController:

- **`/rag/qa/prompt`**: Simple RAG application using Lame Commands and ChromaDB repositories to make request to the LLM including data from the specified collections
- **`/rag/qa/smart/prompt`**: Simple RAG application using Lame Commands to orchestrate the process with the LLM as evaluator, data source selector and augmentation tricks
- **`/chains/example/smart-rag`**: Same process implemented using fluent LameChain (this one is in the `LameSamplesController.cs` controller)

The **`LameSamplesService`** includes a conceptual example (not for execution) showing how to build chains from basic sequences to complex chains with parallel steps or conditional branches.

### Key Implementation Examples:

| File | Purpose |
|------|---------|
| **`LameSamplesController.cs`** | Shows how to use CommandsService for Core Commands. Includes test-case endpoints for Atomic Value Commands and basic usage examples. |
| **`RagService.cs`** | Demonstrates RAG examples using both single Lame Commands and Fluent LameChain—practical examples for real-world scenarios. |
| **`OllamaSharpService.cs`** | Simple implementation example of prompting the LLM using Lame Commands. |

---


## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.