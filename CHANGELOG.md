# Changelog

## [0.1.0](https://github.com/klinki/knowledgebase-platform/compare/v0.0.1...v0.1.0) (2026-03-08)


### Features

* **api:** add health checks for api and queue ([90833cd](https://github.com/klinki/knowledgebase-platform/commit/90833cded022aaf240636e97aa1bdb91ed52ea59))
* **api:** configure hangfire infrastructure ([85264b5](https://github.com/klinki/knowledgebase-platform/commit/85264b52ee673c4b6416f19d6558af9030b0dcab))
* **api:** migrate docs to OpenAPI and Scalar ([78690d5](https://github.com/klinki/knowledgebase-platform/commit/78690d5d3b253ac76160be308bb782f3b8129698))
* **auth:** Add Identity-based authentication flow ([0d05908](https://github.com/klinki/knowledgebase-platform/commit/0d05908884265673d1fcf7044c5f03e434adb3b3))
* **auth:** Harden Angular session handling ([80db43d](https://github.com/klinki/knowledgebase-platform/commit/80db43d89d805263961855ea5f634423754b487c))
* **backend:** add opentelemetry metrics ([3f8aa95](https://github.com/klinki/knowledgebase-platform/commit/3f8aa951d6d4227e527d3ddf2cabb3b1e8dc5032))
* **backend:** add serilog logging infrastructure ([75ea776](https://github.com/klinki/knowledgebase-platform/commit/75ea776b34026ddf96beac5f7223874b4e34c8ea))
* **backend:** Allow CORS headers ([eb41fa2](https://github.com/klinki/knowledgebase-platform/commit/eb41fa2de850b2c0d6201aab33f232de47f0644e))
* **backend:** configure openai api urls from configuration settings ([e4b41bc](https://github.com/klinki/knowledgebase-platform/commit/e4b41bcc68e45d15edfcf455239da97bebe67a12))
* **backend:** split hangfire worker from API process ([6b99687](https://github.com/klinki/knowledgebase-platform/commit/6b99687d479af43758256c195a69f9de8e10b201))
* **build:** add Run target to build script for dev environment setup ([fa6e88c](https://github.com/klinki/knowledgebase-platform/commit/fa6e88c7c545b19177d881002303c170c5404ef8))
* **build:** add unified powershell build script ([fe73720](https://github.com/klinki/knowledgebase-platform/commit/fe737201be55a371e17d7b9a5358271ff569ec8e))
* **deploy:** add dockerized ci cd deployment pipeline ([ce8d423](https://github.com/klinki/knowledgebase-platform/commit/ce8d423bb59e9539d04c3f44e5d2ff298fa76b0b))
* **deploy:** Support shared proxy multi-app hosting ([6446b61](https://github.com/klinki/knowledgebase-platform/commit/6446b61eb5fe65acdf9ddb685a5ee1a96cb6b282))
* **docker:** optimize backend docker deployment ([ad842ac](https://github.com/klinki/knowledgebase-platform/commit/ad842ac0c22e66e2cdf22a5cd379c2663ce70d06))
* **docs:** Formalize release process with release please ([7804591](https://github.com/klinki/knowledgebase-platform/commit/78045916394ab28964c3815857c1c028ef5f9d36))
* **extension:** add webpage capture functionality ([a2eb123](https://github.com/klinki/knowledgebase-platform/commit/a2eb1237deeed0f339c22d427e5a354a4bc02769))
* **extension:** Implement Phase 1 - The Collector browser extension ([6522862](https://github.com/klinki/knowledgebase-platform/commit/6522862bb41d71d74b5906f7cf920d0246512239))
* **frontend:** Add Angular frontend ([516f325](https://github.com/klinki/knowledgebase-platform/commit/516f32517eafa43769f0fa048f49914a3a5d54de))
* **frontend:** Add backend-driven dashboard state ([9eed6ce](https://github.com/klinki/knowledgebase-platform/commit/9eed6cee6c7a884d5f9a7d96aa20a1bf4c0473fe))
* implement ASP.NET Core backend for knowledgebase platform ([a3f2761](https://github.com/klinki/knowledgebase-platform/commit/a3f2761eccef1becdce7e0cb9471d3152a86ff6e))


### Bug Fixes

* align capture accepted response contract ([c137e4e](https://github.com/klinki/knowledgebase-platform/commit/c137e4e50b31430bfe407de5a8f5da3450806ab5))
* **api:** add FluentValidation auto-validation to fix failing integration tests ([e6df864](https://github.com/klinki/knowledgebase-platform/commit/e6df86474f7e28bdd49d3e5e6129d62ecf744a57))
* **api:** emit enum values in OpenAPI schemas ([0a7690c](https://github.com/klinki/knowledgebase-platform/commit/0a7690ca00334f832aef95ad989bfc87d80bb2f5))
* **build:** Apply migrations before dev startup ([7350971](https://github.com/klinki/knowledgebase-platform/commit/73509719bc3691d329e81f8723971bd9d160c56e))
* **docs:** run backend watch in development mode ([2eaee92](https://github.com/klinki/knowledgebase-platform/commit/2eaee921aec6f985113dcaeaa08cbdbd6f9f4a26))
* execute semantic and tag search in database ([80b61f2](https://github.com/klinki/knowledgebase-platform/commit/80b61f26041fdd7a4dda84e99ea4cc2877990017))
* **extension:** Fix source data type to string ([2878612](https://github.com/klinki/knowledgebase-platform/commit/2878612e34334a93ba79acb19d5a110ba23b15ba))
* **ext:** map capture payloads to backend DTO contract ([aff9d96](https://github.com/klinki/knowledgebase-platform/commit/aff9d9606a9603fee2708e8c7a3bd5eef5daa6c2))
* Fix EmbeddingVector configuration ([925e907](https://github.com/klinki/knowledgebase-platform/commit/925e9073d773b2476869fbd2d6d16b6baaff660d))
* Fix vector usage ([da133dd](https://github.com/klinki/knowledgebase-platform/commit/da133ddbc4ce067d309252c9290244c09cb778e5))
* **frontend:** correctly load browser extension scripts ([e791d75](https://github.com/klinki/knowledgebase-platform/commit/e791d75b4c4d8cedb9e9746a2f50fa18cc3977e8))
* queue capture processing in hosted service ([085da5d](https://github.com/klinki/knowledgebase-platform/commit/085da5d0e06fdd5b7d6877b361be23a84ef8a885))
* remove repository-level save changes ([c601be8](https://github.com/klinki/knowledgebase-platform/commit/c601be8049039797532f6bc4c4dc956729a59a59))
