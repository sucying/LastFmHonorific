# LastFmHonorific

Plugin de Dalamud (FFXIV) que atualiza o seu título do **Honorific** com a música
que está tocando "agora" no seu perfil do **Last.fm** — funciona com qualquer
player que faça scrobbling (YouTube Music, Spotify, foobar2000, iTunes, etc),
desde que o Last.fm esteja recebendo os scrobbles.

## Proveniência e créditos

Este projeto **não foi escrito do zero**: é uma adaptação direta do código do
[SpotifyHonorific](https://github.com/Valiice/SpotifyHonorific) (por Valiice),
que por sua vez é um fork do
[DiscordActivityHonorific](https://github.com/anya-hichu/DiscordActivityHonorific)
(por anya-hichu). A arquitetura inteira — comunicação via IPC com o Honorific,
sistema de templates Scriban, cache de templates, gradientes, modo arco-íris,
janela de configuração — vem desses dois projetos. A única parte reescrita foi
a camada que busca "o que está tocando agora": trocamos a integração com a API
do Spotify (que usa OAuth) pela API do Last.fm (que usa só usuário + API key).

A adaptação foi feita com ajuda de um assistente de IA (Claude, da Anthropic),
a partir do código-fonte público do SpotifyHonorific.

Por causa disso, este projeto é distribuído sob a mesma licença do original,
**GNU AGPL-3.0** (veja o arquivo `LICENSE.md`), que exige que qualquer
trabalho derivado mantenha a mesma licença e dê crédito ao trabalho original.
Se você redistribuir ou publicar este plugin (inclusive num repositório de
Dalamud), mantenha esta seção e o arquivo de licença.

**Pré-requisito:** você precisa ter o plugin **Honorific** (por Caraxi) já
instalado e habilitado no seu Dalamud.



---

## Passo a passo (sem instalar nada pesado no PC)

Você vai usar o **GitHub** (a conta web, sem precisar de terminal) para guardar
este código, e o **GitHub Actions** (robô de compilação gratuito do próprio
GitHub) para transformar esse código num plugin de verdade (`.zip`).

### 1. Criar o repositório no GitHub

1. Entre em [github.com/new](https://github.com/new).
2. Em "Repository name", coloque `LastFmHonorific`.
3. Deixe como **Public** (precisa ser público para o Dalamud conseguir baixar
   o plugin depois).
4. Clique em "Create repository". Não marque nenhuma opção de criar README,
   .gitignore etc — deixe vazio.

### 2. Subir os arquivos

1. Na página do repositório recém-criado, clique no link **"uploading an
   existing file"** (ou "Add file" → "Upload files").
2. Arraste **a pasta inteira** que eu te entreguei (ou todos os arquivos e
   subpastas dela) para essa área de upload. O GitHub aceita arrastar pastas
   completas pelo navegador.
3. Escreva qualquer mensagem em "Commit message" (ex: "primeira versão") e
   clique em **"Commit changes"**.

Confira se, depois do upload, a estrutura no GitHub ficou assim na raiz:

```
LastFmHonorific/          <- pasta com o código C#
.github/workflows/build.yml
images/icon.png
LastFmHonorific.sln
pluginmaster.json
README.md
.gitignore
```

### 3. Deixar o robô compilar

1. Assim que você fizer o "Commit changes", o GitHub Actions já vai começar a
   compilar automaticamente (porque o `build.yml` está configurado para isso).
2. Clique na aba **"Actions"** no topo do repositório.
3. Você verá uma execução em andamento (bolinha amarela girando). Clique nela
   e espere terminar — geralmente leva de 2 a 5 minutos.
4. Quando o ícone virar um **check verde**, role a página até **"Artifacts"**
   (no final da página de detalhes da execução).
5. Clique em **"LastFmHonorific-plugin-zip"** para baixar. Dentro desse `.zip`
   tem outro arquivo chamado `latest.zip` — é esse `latest.zip` que é o
   plugin de verdade.

> Se o check ficar vermelho (falhou), me avise e cole aqui o que aparece na
> aba "Actions" — eu te ajudo a resolver.

### 4. Publicar esse zip como uma "Release" (para o Dalamud conseguir baixar)

O Dalamud não consegue instalar o `.zip` direto do seu computador — ele
precisa de uma URL na internet. A forma mais simples é criar uma "Release" no
GitHub:

1. No seu repositório, clique em **"Releases"** (barra lateral direita) →
   **"Create a new release"**.
2. Em "Choose a tag", digite `v1.0.0` e clique em "Create new tag".
3. Em "Release title", coloque `v1.0.0`.
4. Arraste o `latest.zip` que você baixou no passo anterior para a área de
   anexos, mas **renomeie o arquivo para `LastFmHonorific.zip`** antes de
   arrastar (isso é importante, o link que já preparei espera esse nome).
5. Clique em **"Publish release"**.

### 5. Ajustar dois detalhes nos arquivos (substituir o seu usuário)

Antes ou depois de subir, abra estes dois arquivos pelo próprio site do
GitHub (clique no arquivo → ícone de lápis para editar) e troque
`REPLACE_WITH_YOUR_GITHUB_USERNAME` pelo seu nome de usuário real do GitHub,
e `REPLACE_WITH_YOUR_NAME` pelo seu nome/nick:

- `pluginmaster.json`
- `LastFmHonorific/LastFmHonorific.json`
- `LastFmHonorific/LastFmHonorific.csproj` (campo `PackageProjectUrl`)

Depois de editar, é só fazer commit direto pela interface web (botão verde
"Commit changes"). Se você editar **depois** de já ter criado a Release, não
tem problema — o `pluginmaster.json` é só lido quando o Dalamud carrega o
repositório, então da próxima vez que abrir o jogo ele vai pegar a versão
atualizada.

### 6. Adicionar o repositório customizado no jogo

1. Abra o FFXIV, digite no chat: `/xlsettings`
2. Vá na aba **"Experimental"**.
3. Procure a seção **"Custom Plugin Repositories"**.
4. Cole esta URL no campo de texto (troque pelo seu usuário):
   ```
   https://raw.githubusercontent.com/SEU_USUARIO/LastFmHonorific/master/pluginmaster.json
   ```
5. Clique no botão **"+"** ao lado para adicionar, confirme que a caixinha ao
   lado da nova linha está marcada, e clique no ícone de salvar no
   canto inferior direito.

### 7. Instalar o plugin

1. Abra o instalador de plugins: `/xlplugins`
2. Vá em **"All Plugins"**, procure por **"LastFmHonorific"**.
3. Clique em **Install**.

### 8. Configurar o Last.fm

1. Crie uma API key gratuita em
   [last.fm/api/account/create](https://www.last.fm/api/account/create)
   (nome e descrição do app podem ser qualquer coisa, ex: "FFXIV Honorific").
2. Copie a **"API key"** gerada.
3. No jogo, digite `/lastfmhonorific config`.
4. Na aba **"Account"**, cole seu **usuário do Last.fm** e a **API key**.
5. Vá na aba **"Config"**, selecione o config "Last.fm" e cole o template que
   você já usava no SpotifyHonorific no campo "Title Template" — ele deve
   funcionar sem nenhuma alteração, já que as variáveis (`Activity.Name`,
   `Activity.Artists[0].Name`, `Context.SecsElapsed`) são as mesmas.

Pronto — assim que o Last.fm marcar uma música como "scrobbling now" no seu
perfil, o título deve aparecer no jogo em até ~2 segundos.

---

## Atualizando o plugin no futuro

Se você (ou eu) editar o código depois:

1. Suba os arquivos novos para o GitHub (substituindo os antigos).
2. Espere o Actions compilar de novo (aba "Actions").
3. Baixe o novo `latest.zip`.
4. Vá em "Releases" → edite a release `v1.0.0` (ou crie uma nova, ex:
   `v1.0.1`) → substitua o arquivo `LastFmHonorific.zip` pelo novo.
5. No jogo, `/xlplugins` → o Dalamud vai detectar a atualização automaticamente
   na próxima verificação (ou clique em "Check for Updates").

## Comandos no jogo

- `/lastfmhonorific config` — abre a janela de configuração.
- `/lastfmhonorific stats` — mostra estatísticas de uso (chamadas de API, etc).

## Por que isso é mais simples que o Spotify

A API do Last.fm não usa OAuth: não há "client secret", não há tela de login
nem token que expira. Você só precisa de um usuário (o perfil de onde ler "o
que está tocando agora") e uma API key (que identifica o app perante o
Last.fm). Por isso a aba "Account" deste plugin tem só dois campos de texto,
em vez do fluxo de autenticação que o SpotifyHonorific exige.

## Limitações conhecidas

- O Last.fm não informa duração da faixa nem popularidade — por isso
  `Activity.DurationMs` e `Activity.Popularity` sempre aparecem como `0` nos
  templates (mantidos só para compatibilidade com templates antigos do
  SpotifyHonorific que os referenciem).
- Como qualquer integração via scrobbling, pode haver um pequeno atraso entre
  a troca de música e o Last.fm atualizar o "now playing" — isso depende do
  player/app que você usa para ouvir música, não deste plugin.
