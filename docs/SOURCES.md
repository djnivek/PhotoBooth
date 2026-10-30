# Sources officielles consultées

Consultation : **30 octobre 2026**. Les informations de support, tarifs et limites évoluent.
La compatibilité documentaire ne remplace pas l’essai sur la borne.

| Sujet | Source et portée |
| --- | --- |
| Canon SDK, USB, visée, transfert, C# | [Canon Europe](https://www.canon-europe.com/business/imaging-solutions/sdk/) ; accès développeur requis |
| EOS 4000D dans la famille EDSDK | [Liste Canon](https://www.kr.canon/support/sdk/sdkMain) ; page de compatibilité historique, version actuelle du SDK à recouper au téléchargement Europe |
| SDK récent | [Canon Developer Programme Europe](https://developers.canon-europe.com/developers/s/article/Latest-EOS-SDK-Version-13-x) ; accès/récupération de la page non aboutis pendant cette préparation |
| DNP DS620 et pilote Windows 10 | [Téléchargements DNP Europe](https://dnpphoto.eu/downloads/) ; pilote Windows 10 explicitement listé |
| DNP format papier/résolution | [Manuel DS620/820](https://dnpphoto.com/Portals/0/Resources/DP-DS620_820_UserGuide_English_Ver3.0.2.pdf) ; surface du pilote à contrôler sur Windows |
| DSLR Booth/LumaBooth extension | [Triggers et API](https://support.lumasoft.co/en/articles/12831651-triggers-webhooks-and-api) ; version installée non connue |
| Windows 10 Pro 22H2 | [Microsoft Lifecycle](https://learn.microsoft.com/en-us/lifecycle/products/windows-10-home-and-pro) ; fin du support standard le 14 octobre 2025 |
| Éditions et limites Shell Launcher | [Microsoft Shell Launcher](https://learn.microsoft.com/en-us/windows/configuration/shell-launcher/) |
| Kiosque UWP/Edge | [Microsoft Assigned Access](https://learn.microsoft.com/en-us/windows/configuration/assigned-access/) |
| Runtime .NET 10 | [Matrice officielle dotnet/core](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md) ; Windows 10 Pro 22H2 absent |
| Docker sur Mac | [Docker Desktop Mac](https://docs.docker.com/desktop/setup/install/mac-install/) ; ne valide pas les pilotes Windows ni le matériel USB de la borne |
| Édition d’image OpenAI | [Guide OpenAI](https://developers.openai.com/api/docs/guides/image-generation) et [référence edits](https://developers.openai.com/api/reference/typescript/resources/images/methods/edit) |
| Modèles/coût | [Flare](https://developers.openai.com/api/docs/models/gpt-image-2.5-flare), [Sunburst](https://developers.openai.com/api/docs/models/gpt-image-2.5-sunburst) ; disponibilité réelle du compte à vérifier |
| Crédits API liés à ChatGPT | [Bénéfice abonnés OpenAI](https://developers.openai.com/api/docs/guides/chatgpt-api-credits) ; activation, éligibilité et solde de Kevin non vérifiés |
| Supabase Free/Pro | [Pricing](https://supabase.com/pricing), [facturation](https://supabase.com/docs/guides/platform/billing-on-supabase) |
| Durées/mémoire/HTML Edge | [Edge Functions limits](https://supabase.com/docs/guides/functions/limits) ; consulté aussi via MCP search_docs |
| Tâches durables | [Supabase Queues](https://supabase.com/docs/guides/queues/pgmq) ; les effets externes doivent rester idempotents |
| Évolution TLS | [Changelog du 12 octobre 2026](https://supabase.com/changelog/cbc-tls-ciphers-deprecated) ; vérifier le TLS du client Windows avant retrait des anciens chiffrements, annoncé au plus tôt le 10 novembre 2026 |

Les estimations de charge, les tailles par séance, le format de grille, la rétention et
le choix d’architecture sont des propositions de conception ; ils ne sont pas des promesses
ou mesures des fournisseurs.
