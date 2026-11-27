# A simple, cross platform, modulith ecommerce system built on .NET Core [![Join the chat at https://gitter.im/ECommerce/ECommerce](https://badges.gitter.im/ECommerce/ECommerce.svg)](https://gitter.im/ECommerce/ECommerce?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## High level architecture

![SimpleCommerce - Modulith architecture](https://raw.githubusercontent.com/ECommerce/ECommerce/master/modular-architecture.png)

## Build Status
| Build server    | Platform       | Status      |
|-----------------|----------------|-------------|
| Azure Pipelines | All            |[![Build Status](https://ECommerce.visualstudio.com/ECommerce/_apis/build/status/ECommerce.ECommerce?branchName=master)](https://ECommerce.visualstudio.com/ECommerce/_build/latest?definitionId=1&branchName=master)
|Travis           | Linux / MacOS  |[![Build Status](https://travis-ci.org/ECommerce/ECommerce.svg?branch=master)](https://travis-ci.org/ECommerce/ECommerce) |

## Online demo (Azure Website)
- Store front: http://demo.ECommerce.com
- Administration: http://demo.ECommerce.com/admin Email: admin@ECommerce.com Password: 1qazZAQ!

## Docker

For testing purpose only `docker run -p 5000:80 ECommerce/ci-build`

Continuous deployment: https://ci.ECommerce.com

## Visual Studio 2022 and SQL Server

#### Prerequisites

- SQL Server
- Visual Studio 2022 and .NET 8

#### Steps to run

- Update the connection string in appsettings.json in ECommerce.WebHost
- Build the whole solution.
- In Solution Explorer, make sure that ECommerce.WebHost is selected as the Startup Project
- Open the Package Manager Console Window and make sure that ECommerce.WebHost is selected as the Default project. Then type "Update-Database" then press "Enter". This action will create the database schema.
- In Visual Studio, press "Control + F5".
- The back-office can be accessed via /Admin using the following built-in account: admin@ECommerce.com, 1qazZAQ!

## Mac/Linux with PostgreSQL

#### Prerequisite

- PostgreSQL
- [.NET Core SDK 8.0](https://www.microsoft.com/net/download/all)
- Entity Framework Core Tools (`dotnet tool install --global dotnet-ef`)

#### Steps to run

- Update the connection string in appsettings.json in ECommerce.WebHost.
- Run the simpl-build.sh file by issuing the following command: "sudo ./simpl-build.sh". For ubuntu 18: "sudo bash simpl-build.sh"
- In the terminal, navigate to "src/ECommerce.WebHost" and type "dotnet run" and then hit "Enter".
- Open http://localhost:49206 in the browser. The back-office can be accessed via /Admin using the following built-in account: admin@ECommerce.com, 1qazZAQ!

## Technologies and frameworks used:

- ASP.NET Core
- Entity Framework Core
- ASP.NET Identity Core
- Angular 1.6.3
- MediatR 7.0.0 for domain event

## Docs

https://docs.ECommerce.com/

## Roadmap

https://github.com/ECommerce/ECommerce/wiki/Roadmap

## How to contribute

- Star this project on GitHub.
- Report bugs or suggest features by creating new issues or adding comments to issues
- Submit pull requests
- Spread the word by blogging about ECommerce or sharing it on social networks
- Donate to us

## Contributors

This project exists thanks to all the people who contribute.

<a href="https://github.com/ECommerce/ECommerce/graphs/contributors"><img src="https://opencollective.com/ECommerce/contributors.svg?width=890" title="contributors" alt="contributors" /></a>

## Backers

Love our work and help us continue our activities? [[Become a backer](https://opencollective.com/ECommerce#backer)]

<a href="https://opencollective.com/ECommerce#backers" target="_blank"><img src="https://opencollective.com/ECommerce/backers.svg?width=890"></a>

## Sponsors

Become a sponsor and get your logo on our README on Github with a link to your site. [[Become a sponsor](https://opencollective.com/ECommerce#sponsor)]

<a href="https://opencollective.com/ECommerce/sponsor/0/website" target="_blank"><img src="https://opencollective.com/ECommerce/sponsor/0/avatar.svg"></a>
<a href="https://opencollective.com/ECommerce/sponsor/1/website" target="_blank"><img src="https://opencollective.com/ECommerce/sponsor/1/avatar.svg"></a>
<a href="https://opencollective.com/ECommerce/sponsor/2/website" target="_blank"><img src="https://opencollective.com/ECommerce/sponsor/2/avatar.svg"></a>
<a href="https://opencollective.com/ECommerce/sponsor/3/website" target="_blank"><img src="https://opencollective.com/ECommerce/sponsor/3/avatar.svg"></a>
<a href="https://opencollective.com/ECommerce/sponsor/4/website" target="_blank"><img src="https://opencollective.com/ECommerce/sponsor/4/avatar.svg"></a>

## License

ECommerce is licensed under the Apache 2.0 license.
