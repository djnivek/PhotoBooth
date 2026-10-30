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

## Compléments de la livraison anniversaire

- [Avalonia, plateformes supportées](https://docs.avaloniaui.net/docs/overview/supported-platforms) : Windows 10 22H2 Tier 2 ; exécution locale toujours à vérifier.
- [OpenAI outil image](https://developers.openai.com/api/docs/guides/tools-image-generation) et [Responses background](https://developers.openai.com/api/docs/guides/background) : requête asynchrone avec édition xhigh et polling.
- [DeviceCapabilities](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-devicecapabilitiesw), [DocumentProperties](https://learn.microsoft.com/en-us/windows/win32/printdocs/documentproperties), [StartDoc](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-startdocw) : papier du pilote et soumission GDI, pas preuve de feuille imprimée.
- [GitHub Pages](https://docs.github.com/en/pages/getting-started-with-github-pages/creating-a-github-pages-site) : forfait requis pour un dépôt privé ; refus effectif HTTP422 sur ce dépôt le 30 octobre.
- [Éligibilité Canon](https://developercommunity.usa.canon.com/s/article/Who-is-eligible-to-download-development-tools-files-from-this-website) : un développeur en France doit passer par le portail Europe, pas contourner l’éligibilité d’une autre région.
- [Canon B2B SDK](https://industrial.imaging.canon.de/integration/sdk/) : programme gratuit, activation pouvant prendre quelques jours ; récupération non garantie avant l’anniversaire.
- [Stockage Supabase](https://supabase.com/docs/guides/storage/pricing) : 1 Go Free, 100 Go Pro ; ne pas confondre avec le disque Postgres.

- [FC.SDK3.2.1021](https://github.com/SharpAstro/FC.SDK/tree/0800943d2d4aaaa8ffc5da11b5ce57bfed9614a7) : source correspondant au package NuGet, MIT, transports WPD sans changement de pilote ; preuves déclarées 6D/450D, **pas 4000D**. Adaptateur personnel testé contre un faux transport seulement.

## Préparation du focus (Colors Party)

Source primaire examinée : [FC.SDK commit 0800943](https://github.com/SharpAstro/FC.SDK/tree/0800943d2d4aaaa8ffc5da11b5ce57bfed9614a7), version embarquée 3.2.1021. `TakePictureAsync`, `AutoFocusLiveViewAsync`, `CancelAutoFocusAsync`, `GetFocusStateAsync` et `StateChanged/AfResult` ont été examinés. L’ACK d’autofocus n’est pas un état de verrouillage ; la documentation du payload AfResult ne permet pas d’attester son sens sur EOS4000D. Ne pas inventer une valeur de succès ni réutiliser des mesures d’autres boîtiers. La voie MF demande confirmation opérateur et mode MF réel.
