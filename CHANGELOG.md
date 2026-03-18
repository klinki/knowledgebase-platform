# Changelog

## [0.2.0](https://github.com/klinki/knowledgebase-platform/compare/v0.1.0...v0.2.0) (2026-03-18)


### Features

* Add Adminer and Seq to production ([7f1a9f9](https://github.com/klinki/knowledgebase-platform/commit/7f1a9f99743b542a680da2e05fa0fac62c5f74fc))
* Add capture browsing flow ([ab9bf5e](https://github.com/klinki/knowledgebase-platform/commit/ab9bf5ea12a3ce1e55fe67f4ac8a40b04964f6ce))
* Add direct capture creation ([fd8f0d5](https://github.com/klinki/knowledgebase-platform/commit/fd8f0d5cd21173cd8fd6f1be538c8fa41d97c9ba))
* Add invitation-based user onboarding ([78c58fa](https://github.com/klinki/knowledgebase-platform/commit/78c58fa29001fb93611a741e6dd93637416877d8))
* Add server admin CLI ([356335c](https://github.com/klinki/knowledgebase-platform/commit/356335c4ecc04a97b35ac123661a6ebb08d4617e))
* **backend:** Add user-scoped knowledge ownership ([f25e24d](https://github.com/klinki/knowledgebase-platform/commit/f25e24d46078e66065152da06da2732d043dd3e8))
* **deploy:** Add Vertex AI proxy support ([3f6e36a](https://github.com/klinki/knowledgebase-platform/commit/3f6e36acf25a0f2d03751dfad8c428b317ea9c0f))
* **tags:** add tag management page with create, rename, and delete ([73b6873](https://github.com/klinki/knowledgebase-platform/commit/73b6873c79a53c86045b8fb410b0cb49b7d53a68))


### Bug Fixes

* Add capture processing diagnostics ([86b29cb](https://github.com/klinki/knowledgebase-platform/commit/86b29cb109181634262f2ab55b0ae705912f7d6a))
* Address PR review feedback ([af67ad8](https://github.com/klinki/knowledgebase-platform/commit/af67ad8382bb16048fdd039a1129f64586fd848d))
* Fix UX review issues across extension and frontend ([8fd99de](https://github.com/klinki/knowledgebase-platform/commit/8fd99dee6389cf0f4ab0391a8b6b0bcf0d739be6))
* Remove AI fallback processing ([09561f7](https://github.com/klinki/knowledgebase-platform/commit/09561f776bb8ebcca6c212e4b63670dc24fced70))
* Repair container runtimes ([ab84fd4](https://github.com/klinki/knowledgebase-platform/commit/ab84fd43f9dc76fe421a90151897b9290bea7208))
* Repair migrator bundle build ([825b0f3](https://github.com/klinki/knowledgebase-platform/commit/825b0f317faf1fe453650c381728da5627563537))
* Retry failed capture processing ([54075e7](https://github.com/klinki/knowledgebase-platform/commit/54075e7cd540a6df25ff7649ea27766aae149794))
* Use ASP.NET runtime for migrator ([f86a2ef](https://github.com/klinki/knowledgebase-platform/commit/f86a2ef81a054a893508630e0f7410f0d49ec054))

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
