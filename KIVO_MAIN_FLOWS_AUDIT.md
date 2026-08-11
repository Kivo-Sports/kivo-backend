# Kivo - Auditoria Real dos Fluxos Principais

Data da auditoria: 2026-08-10  
Escopo: backend .NET, frontend Next.js, SQL Server via Docker, fluxos principais por perfil.

## Execucao

- Backend validado em `http://localhost:5211`.
- Frontend validado em `http://localhost:3000`.
- Banco SQL Server local validado com dados reais de auditoria e seeds.
- `dotnet build kivoBackend.sln --no-restore`: OK, 0 erros.
- `npm run build`: OK.
- `npm run lint`: falhou com 3322 problemas, incluindo 15 erros de ESLint. Nao bloqueou a build, mas deve entrar na fila de qualidade.
- Evidencias HTTP reais coletadas por `curl` contra a API local. Tokens omitidos deste relatorio.

## Resumo - Torcedor

Fluxos encontrados:
- Cadastro de torcedor
- Login pos-cadastro
- Validacao previa de email/CPF
- Consulta e edicao de perfil
- Exclusao/desativacao por usuario

Fluxos validados:
- `POST /api/usuario/torcedor`
- `POST /api/auth/login`
- `POST /api/usuario/check-email`
- `PUT /api/Usuario/torcedor/{id}`
- `GET /api/Usuario/{id}`

Fluxos OK:
- Cadastro por API retornou `201`.
- Login por API retornou `200`.
- Persistencia basica de usuario, CPF, endereco e cargo funcionou.

Fluxos com problemas:
- Validacao previa de email/CPF do frontend chama endpoints protegidos antes do login.
- Usuario autenticado consegue editar perfil de outro usuario se souber o `id`.

Criticos: 1  
Altos: 0  
Medios: 1  
Baixos: 0

### Fluxos validados sem problemas encontrados

- Cadastro feliz de torcedor via API.
- Login feliz de torcedor via API.
- Persistencia basica do perfil Torcedor.

## [CRITICO] Edicao de perfil de outro usuario por IDOR

**Fluxo:** Torcedor > perfil > editar dados pessoais

**Evidencia:** usuario Torcedor A autenticado editou usuario Torcedor B por `PUT /api/Usuario/torcedor/{id}`. A criacao do usuario B retornou `201`, a edicao feita com token de A retornou `200`, e `GET /api/Usuario/{id}` confirmou `nome`, `email`, `telefone` e endereco alterados para dados enviados por A.

**Cenario**

Um usuario autenticado informa o `id` de outro usuario na URL de edicao de perfil. O backend valida apenas que ha um JWT, mas nao valida se o `id` da rota pertence ao usuario autenticado ou a um administrador.

**Como reproduzir**

1. Criar dois torcedores temporarios.
2. Fazer login como o primeiro torcedor.
3. Enviar `PUT /api/Usuario/torcedor/{idDoSegundoTorcedor}` com o token do primeiro.
4. Consultar `GET /api/Usuario/{idDoSegundoTorcedor}`.

**Esperado**

O backend deve retornar `403 Forbidden` ou `404 Not Found` para usuarios que tentam editar recurso de outro usuario.

**Atual**

O backend retorna `200 OK` e persiste os dados enviados.

**Frontend**

- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/services/registration.service.ts:377`
- O frontend faz login automatico apos cadastro, o que torna o usuario apto a chamar endpoints autenticados.

**Backend**

- `kivoBackend.Presentation/Controller/UsuarioController.cs:217`
- `kivoBackend.Presentation/Controller/UsuarioController.cs:232`
- `kivoBackend.Presentation/Controller/UsuarioController.cs:247`
- As rotas de edicao tem `[Authorize]`, mas passam diretamente o `id` da URL para `_usuarioService.EditarDadosUsuario`.
- `kivoBackend.Presentation/Controller/UsuarioController.cs:372`
- A exclusao tambem usa apenas `[Authorize]` e recebe `id` arbitrario.

**Banco**

- `kivoBackend.Infrastructure/Data/AppDbContext.cs:39`
- O banco modela perfis 1:1 por usuario, mas autorizacao de dono nao e uma restricao de banco. Precisa estar na camada de aplicacao/API.

**Causa raiz**

Falta de checagem de propriedade do recurso. O controller confia no `id` da rota e nao compara com o `ClaimTypes.NameIdentifier` do token nem exige role administrativa.

**Impacto**

Qualquer usuario autenticado pode alterar dados pessoais de outro usuario, incluindo email e endereco. Isso afeta privacidade, integridade cadastral e recuperacao de conta.

**Correcao recomendada**

Extrair o usuario autenticado do JWT, permitir edicao apenas quando `routeId == authenticatedUserId` ou quando o usuario tiver role `Administrador`, e cobrir `PUT`/`DELETE` de todos os perfis com testes de autorizacao.

## [MEDIO] Validacao previa de email e CPF falha para usuario nao autenticado

**Fluxo:** Torcedor > cadastro > validacao de etapas

**Evidencia:** chamada anonima para `POST /api/usuario/check-email` retornou `401` com mensagem de login obrigatorio. O frontend chama essa rota antes do usuario existir e, ao receber erro, retorna `false`.

**Cenario**

Durante o cadastro, o frontend tenta verificar se email e CPF ja existem. Essas rotas exigem autenticacao, entao a validacao previa nunca funciona para um visitante criando conta.

**Como reproduzir**

1. Acessar cadastro de torcedor deslogado.
2. Preencher email/CPF ja existentes.
3. Avancar nas etapas.
4. Observar que a validacao previa nao bloqueia; o erro so aparece no POST final, quando o backend validar duplicidade.

**Esperado**

Validacoes publicas de cadastro devem funcionar antes do login, ou o frontend deve remover essa checagem previa e confiar apenas no POST final com mensagens claras.

**Atual**

O frontend chama endpoints protegidos, recebe `401`, ignora o erro e considera que email/CPF nao existem.

**Frontend**

- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/app/cadastro/[tipo]/page.tsx:192`
- As etapas 1 e 2 chamam `checkEmailExists` e `checkCPFExists`.
- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/services/registration.service.ts:435`
- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/services/registration.service.ts:464`
- As funcoes fazem `fetch` anonimo e retornam `false` em respostas nao OK.

**Backend**

- `kivoBackend.Presentation/Controller/UsuarioController.cs:387`
- `kivoBackend.Presentation/Controller/UsuarioController.cs:405`
- As rotas `check-email` e `check-cpf` possuem `[Authorize]`.

**Banco**

- `kivoBackend.Infrastructure/Data/AppDbContext.cs:34`
- CPF tem indice unico, entao o POST final ainda protege contra duplicidade de CPF.

**Causa raiz**

Contrato divergente entre frontend e backend: o frontend trata a verificacao como publica, o backend trata como autenticada.

**Impacto**

Experiencia ruim no cadastro, etapas aprovadas com dados duplicados, mensagens tardias e chamadas HTTP desnecessarias. Nao observei bypass final de CPF/email porque o backend ainda valida no POST.

**Correcao recomendada**

Escolher um contrato: tornar as rotas de checagem publicas com rate limit e resposta minima, ou remover a checagem previa do frontend e melhorar a exibicao dos erros do POST final.

## Resumo - Organizador de Time

Fluxos encontrados:
- Cadastro de organizador de time
- Login pos-cadastro
- Criacao de time
- Listagem de times do organizador
- Edicao/status/exclusao de time
- Convites pendentes de campeonato
- Resposta a convite

Fluxos validados:
- Criacao/login de dois organizadores de time.
- Criacao de time pelo contrato atual do frontend.
- Criacao de time por contrato completo da API.
- Criacao de time usando `OrganizadorTimeId` de outro usuario.
- Edicao/status de time de outro organizador.

Fluxos OK:
- Cadastro e login do perfil OrganizadorTime funcionaram por API.
- `GET /api/time/organizador` possui filtro pelo organizador autenticado.

Fluxos com problemas:
- Criacao de time pelo frontend falha por ausencia de `EsporteId`.
- API permite criar/editar/ativar/desativar/excluir time de outro organizador.

Criticos: 1  
Altos: 1  
Medios: 0  
Baixos: 0

### Fluxos validados sem problemas encontrados

- Cadastro feliz de OrganizadorTime.
- Login feliz de OrganizadorTime.
- Listagem filtrada de times do organizador em `GET /api/time/organizador`.

## [CRITICO] Organizador consegue criar e alterar times de outro organizador

**Fluxo:** Organizador de Time > gestao de times

**Evidencia:** token do Organizador A criou time usando `OrganizadorTimeId` do Organizador B e recebeu `201`. Depois, token do Organizador B editou time pertencente ao Organizador A e recebeu `200`; o `GET /api/Time/{id}` confirmou nome, cidade e `ativo=false` alterados.

**Cenario**

O frontend envia o `OrganizadorTimeId` no corpo e a API aceita esse valor. Nas rotas de atualizacao/status/delete, a API recebe apenas o `id` do time e nao verifica se o time pertence ao usuario autenticado.

**Como reproduzir**

1. Criar e logar dois usuarios `OrganizadorTime`.
2. Com token A, enviar `POST /api/Time` com `OrganizadorTimeId` do perfil B.
3. Com token B, enviar `PUT /api/Time/{idDoTimeDeA}`.
4. Com token B, enviar `PATCH /api/Time/{idDoTimeDeA}/status`.
5. Consultar o time alterado.

**Esperado**

O servidor deve derivar o organizador pelo token e bloquear qualquer alteracao de time que nao pertence ao usuario autenticado.

**Atual**

As chamadas retornam sucesso e persistem alteracoes em recursos de terceiros.

**Frontend**

- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/store/api/timeApi.ts:13`
- O cliente envia `OrganizadorTimeId` dentro do `FormData`.
- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/app/(dashboard)/organizador/times/criar/page.tsx:123`
- A tela usa `perfil.organizadorTimeId`, mas isso e apenas uma conveniencia de UI, nao controle de seguranca.

**Backend**

- `kivoBackend.Presentation/Controller/TimeController.cs:109`
- `kivoBackend.Presentation/Controller/TimeController.cs:133`
- O `POST` usa `dto.OrganizadorTimeId` fornecido pelo cliente.
- `kivoBackend.Presentation/Controller/TimeController.cs:152`
- `kivoBackend.Presentation/Controller/TimeController.cs:179`
- `kivoBackend.Presentation/Controller/TimeController.cs:195`
- `PUT`, status e `DELETE` nao validam ownership.

**Banco**

- `kivoBackend.Infrastructure/Data/AppDbContext.cs:84`
- O banco garante relacao com esporte, mas nao garante que o usuario autenticado e dono do `OrganizadorTimeId`.

**Causa raiz**

Autorizacao de dono ausente. O backend confia em IDs controlados pelo cliente e executa operacoes por identificador global.

**Impacto**

Um organizador pode sequestrar times, alterar dados, desativar ou remover times de outras contas. Isso compromete o fluxo principal de administracao de equipes.

**Correcao recomendada**

No backend, derivar `OrganizadorTimeId` a partir do usuario autenticado. Em `PUT`, `PATCH` e `DELETE`, buscar o time e exigir `time.OrganizadorTimeId == perfilDoToken.Id` ou role administrativa. Remover `OrganizadorTimeId` do contrato publico de criacao, ou ignorar o valor enviado.

## [ALTO] Criacao de time pelo frontend nao atende ao contrato do backend

**Fluxo:** Organizador de Time > criar time

**Evidencia:** reproduzido `POST /api/Time` com o mesmo formato do frontend: `OrganizadorTimeId`, `Nome`, `Cidade`, `Estado` e logo/URL, sem `EsporteId`. A API retornou `400` com mensagem `Selecione um esporte para o time.`

**Cenario**

Apos cadastro, o organizador de time e redirecionado para criar time. A tela nao possui campo de esporte e o tipo/request do frontend nao envia `EsporteId`, mas o backend exige esse campo.

**Como reproduzir**

1. Cadastrar/logar como OrganizadorTime.
2. Acessar `/organizador/times/criar`.
3. Preencher nome, cidade, estado e logo.
4. Submeter o formulario.

**Esperado**

O time deve ser criado, ou a tela deve solicitar todos os campos obrigatorios pelo backend.

**Atual**

O request nao contem `EsporteId`, logo o backend rejeita a criacao.

**Frontend**

- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/store/api/timeApi.ts:4`
- `buildTimeFormData` nao adiciona `EsporteId`.
- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/app/(dashboard)/organizador/times/criar/page.tsx:32`
- O schema so valida `nome`, `cidade` e `estado`.
- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/app/(dashboard)/organizador/times/criar/page.tsx:133`
- O submit envia dados sem esporte.

**Backend**

- `kivoBackend.Presentation/Controller/TimeController.cs:128`
- O backend rejeita `Guid.Empty` em `EsporteId`.

**Banco**

- `kivoBackend.Infrastructure/Data/AppDbContext.cs:84`
- `Time` possui FK obrigatoria para `Esporte`.

**Causa raiz**

Contrato de criacao divergente: backend modelou time com modalidade obrigatoria, mas o frontend nao implementou selecao/envio desse campo.

**Impacto**

Novo organizador de time fica bloqueado no primeiro fluxo apos cadastro.

**Correcao recomendada**

Adicionar selecao de esporte no frontend usando endpoint de esportes e enviar `EsporteId` no `FormData`. Alternativamente, se o produto definir esporte em outro lugar, o backend deve derivar esse valor e documentar o contrato.

## Resumo - Organizador de Campeonato

Fluxos encontrados:
- Cadastro de organizador de campeonato
- Login pos-cadastro
- Criacao de campeonato
- Edicao/status/cancelamento/exclusao
- Convite de time
- Listagem de convites
- Resposta de convite por organizador de time

Fluxos validados:
- Criacao/login de dois organizadores de campeonato.
- Criacao de campeonato pelo contrato atual do frontend.
- Criacao de campeonato por contrato completo da API.
- Criacao/edicao/abertura de campeonato de outro organizador.
- Convite duplicado para mesmo time.
- Resposta anonima de convite.
- Persistencia de datas invalidas.

Fluxos OK:
- Cadastro e login do perfil OrganizadorCampeonato funcionaram por API.
- Criacao por API funciona quando o contrato correto em `multipart/form-data` e usado com `EsporteId`.

Fluxos com problemas:
- Criacao pelo frontend retorna `415`.
- API permite controle de campeonato de outro organizador.
- Resposta de convite nao exige autenticacao.
- Convites duplicados sao persistidos.
- API aceita campeonato com `DataFim` anterior a `DataInicio`.

Criticos: 2  
Altos: 3  
Medios: 0  
Baixos: 0

### Fluxos validados sem problemas encontrados

- Cadastro feliz de OrganizadorCampeonato.
- Login feliz de OrganizadorCampeonato.
- Criacao de campeonato por API quando enviado o contrato completo esperado pelo backend.

## [CRITICO] Organizador consegue controlar campeonatos de outro organizador

**Fluxo:** Organizador de Campeonato > gestao de campeonatos

**Evidencia:** token do OrganizadorCampeonato A criou campeonato usando `OrganizadorCampeonatoId` do OrganizadorCampeonato B e recebeu `201`. Token B editou campeonato de A com `200`, abriu inscricoes do campeonato de A com `200` e convidou time para campeonato de A com `200`.

**Cenario**

As rotas de campeonato aceitam IDs globais e nao validam se o campeonato pertence ao organizador autenticado.

**Como reproduzir**

1. Criar e logar dois organizadores de campeonato.
2. Com token A, enviar `POST /api/Campeonato` com `OrganizadorCampeonatoId` de B.
3. Com token B, enviar `PUT /api/Campeonato/{idDoCampeonatoDeA}`.
4. Com token B, enviar `PATCH /api/Campeonato/{idDoCampeonatoDeA}/abrir-inscricoes`.
5. Com token B, enviar `POST /api/Campeonato/convidar-time` para campeonato de A.

**Esperado**

Somente o dono do campeonato, ou administrador, deve poder criar, editar, abrir, iniciar, cancelar, excluir e convidar times.

**Atual**

Operacoes de outro organizador retornam sucesso e alteram estado persistido.

**Frontend**

- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/store/api/campeonatoApi.ts:19`
- O cliente envia `organizadorCampeonatoId`.
- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/app/(dashboard)/organizador/campeonatos/criar/page.tsx:121`
- A tela usa o id do perfil, mas isso nao e controle de autorizacao.

**Backend**

- `kivoBackend.Presentation/Controller/CampeonatoController.cs:49`
- `kivoBackend.Presentation/Controller/CampeonatoController.cs:81`
- Criacao usa `dto.OrganizadorCampeonatoId` fornecido pelo cliente.
- `kivoBackend.Presentation/Controller/CampeonatoController.cs:105`
- `kivoBackend.Presentation/Controller/CampeonatoController.cs:161`
- `kivoBackend.Presentation/Controller/CampeonatoController.cs:185`
- `kivoBackend.Presentation/Controller/CampeonatoController.cs:201`
- `kivoBackend.Presentation/Controller/CampeonatoController.cs:221`
- Rotas de alteracao/status/remocao/convite nao verificam dono.

**Banco**

- `kivoBackend.Infrastructure/Migrations/AppDbContextModelSnapshot.cs:254`
- O campeonato possui `OrganizadorCampeonatoId`, mas sem regra que conecte esse valor ao usuario autenticado.

**Causa raiz**

Broken Access Control por ausencia de verificacao de ownership no backend e uso de IDs enviados pelo cliente.

**Impacto**

Um organizador pode alterar calendario, abrir/cancelar competicoes e convidar times em campeonatos de terceiros.

**Correcao recomendada**

Derivar o `OrganizadorCampeonatoId` pelo token no backend. Criar guard central para campeonato: buscar campeonato, comparar `OrganizadorCampeonatoId` com perfil do usuario autenticado e permitir excecao apenas para administrador. Cobrir todas as rotas mutaveis.

## [CRITICO] Resposta de convite de campeonato aceita chamada anonima

**Fluxo:** Organizador de Time > convites de campeonato > responder convite

**Evidencia:** `PATCH /api/Campeonato/responder-convite/{participacaoId}` sem header `Authorization` retornou `200` e alterou participacao para `Aceito`. A listagem posterior mostrou um convite `Aceito` com `RespondidoEm` preenchido.

**Cenario**

Qualquer cliente que conheca um `participacaoId` consegue aceitar ou recusar convite em nome de um organizador de time, informando `OrganizadorTimeId` no corpo.

**Como reproduzir**

1. Criar campeonato e time do mesmo esporte.
2. Enviar convite para o time.
3. Capturar `participacaoId` na listagem de convites.
4. Sem token, enviar `PATCH /api/Campeonato/responder-convite/{participacaoId}` com `{ "organizadorTimeId": "...", "aceito": true }`.
5. Listar convites do campeonato.

**Esperado**

Endpoint deve exigir JWT de OrganizadorTime dono do time convidado, ou administrador.

**Atual**

Endpoint nao exige autenticacao e confia no `OrganizadorTimeId` enviado no corpo.

**Frontend**

- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/store/api/campeonatoApi.ts:70`
- O cliente envia `body` com dados de resposta. Mesmo que o frontend esteja autenticado, a API exposta nao exige isso.

**Backend**

- `kivoBackend.Presentation/Controller/CampeonatoController.cs:229`
- A action `Responder` nao possui `[Authorize]`.
- `kivoBackend.Application/Services/CampeonatoService.cs:112`
- O service atualiza a participacao e grava `RespondidoPorOrganizadorTimeId` a partir do valor recebido.

**Banco**

- `kivoBackend.Infrastructure/Migrations/AppDbContextModelSnapshot.cs:298`
- `RespondidoEm` e `RespondidoPorOrganizadorTimeId` sao persistidos sem trilha de usuario autenticado.

**Causa raiz**

Endpoint publico para operacao mutavel sensivel e ausencia de verificacao de dono do time convidado.

**Impacto**

Convites podem ser aceitos ou recusados por terceiros, afetando chaveamento, participantes e credibilidade dos campeonatos.

**Correcao recomendada**

Adicionar `[Authorize(Roles = "OrganizadorTime,Administrador")]`, derivar `OrganizadorTimeId` pelo token, validar que a participacao pertence a um time daquele organizador e bloquear respostas duplicadas.

## [ALTO] Criacao de campeonato pelo frontend usa contrato incompativel com a API

**Fluxo:** Organizador de Campeonato > criar campeonato

**Evidencia:** reproduzido `POST /api/Campeonato` como o frontend faz hoje, enviando JSON. A API retornou `415 Unsupported Media Type`. Alem disso, o payload do frontend nao envia `EsporteId` nem `FormatoCampeonato`, exigidos pelo backend/modelo.

**Cenario**

A tela de criacao envia um objeto JSON, enquanto o controller espera `[FromForm] CriarCampeonatoDto`. O formulario tambem nao possui selecao de esporte/formato.

**Como reproduzir**

1. Logar como OrganizadorCampeonato.
2. Acessar `/organizador/campeonatos/criar`.
3. Preencher nome, datas e pontuacao.
4. Submeter.

**Esperado**

Campeonato deve ser criado com todos os campos obrigatorios do backend.

**Atual**

O request falha em `415` antes mesmo das validacoes de negocio.

**Frontend**

- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/store/api/campeonatoApi.ts:15`
- O mutation envia JSON.
- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/store/api/campeonatoApi.ts:19`
- Payload nao inclui `esporteId`, `formatoCampeonato`, `quantidadeTimesClassificam` ou logo em `FormData`.
- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/app/(dashboard)/organizador/campeonatos/criar/page.tsx:121`
- Submit envia somente organizador, nome, datas e pontuacoes.

**Backend**

- `kivoBackend.Presentation/Controller/CampeonatoController.cs:49`
- A action espera `[FromForm]`.
- `kivoBackend.Presentation/Controller/CampeonatoController.cs:76`
- O backend exige `EsporteId`.

**Banco**

- `kivoBackend.Infrastructure/Migrations/AppDbContextModelSnapshot.cs:241`
- `EsporteId` e coluna obrigatoria do campeonato.
- `kivoBackend.Infrastructure/Migrations/AppDbContextModelSnapshot.cs:244`
- `FormatoCampeonato` e persistido como inteiro.

**Causa raiz**

Contrato divergente entre frontend e backend para criacao de campeonato.

**Impacto**

Novo organizador de campeonato fica bloqueado no fluxo principal de criacao.

**Correcao recomendada**

Unificar contrato. Se a API continuar `[FromForm]`, o frontend deve montar `FormData` com `OrganizadorCampeonatoId`, `EsporteId`, `FormatoCampeonato`, datas, pontuacoes e logo opcional. Se a API aceitar JSON, ajustar controller e DTO explicitamente.

## [ALTO] Campeonato aceita datas invalidas no backend

**Fluxo:** Organizador de Campeonato > criar/editar campeonato

**Evidencia:** chamada direta para `POST /api/Campeonato` com `DataInicio=2026-12-20` e `DataFim=2026-12-10` retornou `201` e o campeonato foi persistido com fim anterior ao inicio.

**Cenario**

O frontend valida `dataFim > dataInicio`, mas o backend nao repete essa regra. Qualquer cliente alternativo, bug de frontend ou integracao pode gravar campeonatos inconsistentes.

**Como reproduzir**

1. Logar como OrganizadorCampeonato.
2. Enviar `multipart/form-data` valido para `POST /api/Campeonato`.
3. Informar `DataFim` anterior a `DataInicio`.
4. Observar `201 Created`.

**Esperado**

Backend deve retornar `400` e impedir persistencia.

**Atual**

Backend persiste o campeonato com datas invertidas.

**Frontend**

- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/app/(dashboard)/organizador/campeonatos/criar/page.tsx:67`
- A tela valida `dataFim` posterior a `dataInicio`.

**Backend**

- `kivoBackend.Presentation/Controller/CampeonatoController.cs:81`
- O controller copia datas do DTO sem validar ordem.
- `kivoBackend.Application/Services/CampeonatoService.cs:158`
- A edicao tambem atualiza `DataInicio` e `DataFim` sem validar consistencia.

**Banco**

- `kivoBackend.Infrastructure/Migrations/AppDbContextModelSnapshot.cs:232`
- `DataFim` e `DataInicio` sao colunas simples sem check constraint.

**Causa raiz**

Regra de negocio existe apenas no frontend.

**Impacto**

Campeonatos podem aparecer com calendario impossivel, quebrar ordenacoes, filtros e etapas futuras de inscricao/partidas.

**Correcao recomendada**

Adicionar validacao no backend para criacao e edicao. Considerar check constraint no banco se SQL Server for a fonte final de integridade temporal.

## [ALTO] Convites duplicados para o mesmo time no mesmo campeonato

**Fluxo:** Organizador de Campeonato > convidar time

**Evidencia:** duas chamadas consecutivas para convidar o mesmo time ao mesmo campeonato retornaram `200`. A listagem posterior trouxe dois registros para o mesmo `TimeId`: um `Aceito` e outro `Pendente`.

**Cenario**

O service cria sempre uma nova linha `CampeonatoTime` e o banco permite multiplos registros para o mesmo par campeonato/time.

**Como reproduzir**

1. Criar campeonato e time do mesmo esporte.
2. Enviar `POST /api/Campeonato/convidar-time` com o mesmo `campeonatoId` e `timeId`.
3. Repetir a mesma chamada.
4. Listar `GET /api/Campeonato/{campeonatoId}/convites`.

**Esperado**

A segunda chamada deve retornar conflito ou idempotencia sem criar novo vinculo.

**Atual**

O banco fica com duplicidade para o mesmo time/campeonato.

**Frontend**

- `/Users/murilomayervannouhuys/Documents/projetos/kivo/kivo-frontend/src/store/api/campeonatoApi.ts:51`
- A mutation envia apenas `campeonatoId` e `timeId`; nao ha protecao contra duplicidade no contrato.

**Backend**

- `kivoBackend.Presentation/Controller/CampeonatoController.cs:221`
- Controller encaminha o convite sem validacao.
- `kivoBackend.Application/Services/CampeonatoService.cs:54`
- Service valida existencia e esporte, mas nao verifica convite ja existente.
- `kivoBackend.Application/Services/CampeonatoService.cs:67`
- Sempre cria novo `CampeonatoTime`.

**Banco**

- `kivoBackend.Infrastructure/Data/AppDbContext.cs:98`
- A tabela associativa tem chave primaria em `Id`.
- `kivoBackend.Infrastructure/Migrations/AppDbContextModelSnapshot.cs:307`
- Snapshot mostra indices separados em `CampeonatoId` e `TimeId`, sem indice unico composto.

**Causa raiz**

Falta de regra idempotente no service e falta de indice unico `(CampeonatoId, TimeId)` no banco.

**Impacto**

Um time pode ter multiplos convites/participacoes no mesmo campeonato, gerando contagem errada de participantes, convites pendentes eternos e inconsistencia no inicio do campeonato.

**Correcao recomendada**

Antes de criar convite, buscar vinculo existente para o par campeonato/time e retornar conflito ou o estado atual. Adicionar migracao com indice unico composto para impedir duplicidade concorrente.

## Matriz Final

| Perfil | Fluxo | Severidade | Problema | Status |
| --- | --- | --- | --- | --- |
| Torcedor | Perfil | CRITICO | Edicao de perfil de outro usuario por IDOR | Confirmado |
| Torcedor | Cadastro | MEDIO | Validacao previa de email/CPF falha anonima | Confirmado |
| Organizador de Time | Times | CRITICO | Criar/editar/status/delete de time sem ownership | Confirmado |
| Organizador de Time | Criar time | ALTO | Frontend nao envia `EsporteId` exigido | Confirmado |
| Organizador de Campeonato | Campeonatos | CRITICO | Controle de campeonato de outro organizador | Confirmado |
| Organizador de Campeonato | Convites | CRITICO | Resposta de convite aceita chamada anonima | Confirmado |
| Organizador de Campeonato | Criar campeonato | ALTO | Frontend envia JSON, backend espera FormData | Confirmado |
| Organizador de Campeonato | Datas | ALTO | API aceita `DataFim < DataInicio` | Confirmado |
| Organizador de Campeonato | Convites | ALTO | Convites duplicados para mesmo time/campeonato | Confirmado |

## Priorizacao

### P0 - corrigir antes de qualquer validacao com usuarios reais

- Corrigir ownership/IDOR em usuarios, times e campeonatos.
- Proteger `PATCH /api/Campeonato/responder-convite/{participacaoId}` com `[Authorize]` e validacao de dono do time.

### P1 - desbloquear fluxos principais

- Alinhar contrato de criacao de time incluindo `EsporteId`.
- Alinhar contrato de criacao de campeonato entre JSON/FormData e campos obrigatorios.
- Validar datas de campeonato no backend.
- Impedir convites duplicados no service e no banco.

### P2 - qualidade e experiencia

- Ajustar validacao previa de email/CPF no cadastro.
- Corrigir erros de lint reportados pelo frontend.
- Criar testes automatizados E2E/API para os fluxos auditados.
