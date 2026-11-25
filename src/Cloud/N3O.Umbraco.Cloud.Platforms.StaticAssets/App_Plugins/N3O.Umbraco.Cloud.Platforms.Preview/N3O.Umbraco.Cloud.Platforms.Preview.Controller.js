angular.module("umbraco").controller("N3O.Umbraco.Cloud.Platforms.Preview",
    async function ($scope, editorState, contentEditingHelper) {
        $scope.previousETag = null;

        let platformsJsSettings = null;

        window.setInterval(async function() {
            await loadPreviewAsync(editorState, contentEditingHelper)
        }, 10000);

        await loadPreviewAsync(editorState, contentEditingHelper)

        async function loadPreviewAsync(editorState, contentEditingHelper) {
            if (!editorState.current.apps.find(x => x.alias === 'platformsPreview')) {
                return;
            }
            
            let currentVariant = editorState.current.variants.find(v => v.active);

            let properties = contentEditingHelper.getAllProps(currentVariant);
            let apiReq = getApiReq(properties, editorState.current.contentTypeAlias);
            
            populateMetadata(apiReq, editorState.current)

            let settings = await getPlatformsJsSettingsAsync();

            if (settings === null) {
                return;
            }

            let apiRes = await fetch(`/umbraco/backoffice/api/platformsBackOffice/previewHtml/${editorState.current.contentTypeAlias}`, {
                method: "POST",
                headers: {
                    "accept": "application/json",
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(apiReq)
            });

            let res = await apiRes.json();

            if (res.eTag === $scope.previousETag) {
                return;
            }

            $scope.previousETag = res.eTag;

            let container = document.getElementById("platformsPreviewContainer");

            container.innerHTML = "";

            let iframe = document.createElement("iframe");
            iframe.style.width = "100%";
            iframe.style.aspectRatio = "16 / 9";
            iframe.style.border = "0";
            iframe.style.transform = "scale(0.9)";
            iframe.style.transformOrigin = "0 0";
            iframe.style.display = "none";

            container.appendChild(iframe);

            let doc = iframe.contentWindow.document;
            doc.open();
            doc.write(res.html);
            doc.close();

            let script = doc.createElement("script");
            script.src = settings.url;
            script.type = "module";
            script.setAttribute("data-subscription", settings.subscriptionCode);

            doc.body.appendChild(script);

            window.setInterval(function () {
                iframe.style.display = "block";
                container.style.display = "block";
            }, 2000);
        }

        async function getPlatformsJsSettingsAsync() {
            if (platformsJsSettings === null) {
                try {
                    let subscriptionCodeRes = await fetch(`/umbraco/backoffice/api/cloudBackOffice/subscription/code`);
                    let urlRes = await fetch(`/umbraco/backoffice/api/platformsBackOffice/platformsJs/url`);

                    if (subscriptionCodeRes.ok && urlRes.ok) {
                        platformsJsSettings = {
                            subscriptionCode: await subscriptionCodeRes.json(),
                            url: await urlRes.json()
                        };
                    }
                } catch {
                    platformsJsSettings = null;
                }
            }

            return platformsJsSettings;
        }

        function populateMetadata(apiReq, content) {
            let currentVariant = editorState.current.variants.find(v => v.active);

            apiReq["name"] = currentVariant.name;
            apiReq["key"] = content.key;
            apiReq["parentId"] = content.parentId;
        }

        function getApiReq(poperties, contentTypeAlias) {
            let req = {};

            poperties.forEach(property => {
                req[property.alias] = property.value
            });
            
            req['contentTypeAlias'] = contentTypeAlias;

            return req;
        }
    });
