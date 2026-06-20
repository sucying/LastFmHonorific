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
