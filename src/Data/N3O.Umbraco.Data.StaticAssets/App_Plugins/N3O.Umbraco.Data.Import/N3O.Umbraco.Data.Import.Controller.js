angular.module("umbraco")
    .controller("N3O.Umbraco.Data.Import", function ($scope, editorState, contentResource, assetsService) {
        $scope.startOver = function () {
            $scope.processing = false;
            $scope.contentType = null;
            $scope.errorMessages = null;
            $scope.show = "form";
        };

        (async () => {
            $scope.startOver();
            
            $scope.content = await contentResource.getById(editorState.current.id);
            $scope.contentTypes = await getContentTypes($scope.content.key);
            $scope.moveUpdatedContentToCurrentLocation = false;

            fetch("/umbraco/backoffice/api/Imports/lookups/datePatterns", {
                headers: {
                    "Accept": "application/json"
                }
            })
            .then(res => res.json())
            .then(res => {
                $scope.datePatterns = res;
                $scope.datePattern = res[0];
            });
        })();

        $scope.refreshProperties = function() {
            fetch(`/umbraco/backoffice/api/Imports/importableProperties/${$scope.contentType.alias}`, {
                headers: {
                    "Accept": "application/json",
                }
            })
                .then(res => res.json())
                .then(res => {
                    for (let property of res) {
                        property.selected = false;
                    }

                    $scope.importableProperties = res;
                });
        };
        
        $scope.getTemplate = async function () {
            let selectedPropertyAliases = $scope.importableProperties.filter(x => x.selected).map(x => x.alias);

            if (!selectedPropertyAliases.length) {
                processingError("At least one property must be selected");

                return;
            }
            
            let req = {
                properties: selectedPropertyAliases
            };
            
            const getTemplate = await fetch(`/umbraco/backoffice/api/Imports/template/${$scope.contentType.alias}`, {
                method: "POST",
                headers: {
                    "Accept": "*/*",
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(req)
            });
            
            const blob = await getTemplate.blob();
            const header = getTemplate.headers.get("Content-Disposition");
            const parts = header.split(";");
            const filename = parts[1].split("=")[1].replaceAll('"', '');

            const newBlob = new Blob([blob]);
            const blobUrl = window.URL.createObjectURL(newBlob);
            const link = document.createElement("a");
            link.href = blobUrl;
            link.setAttribute("download", filename);
            document.body.appendChild(link);
            link.click();
            link.parentNode.removeChild(link);
            window.URL.revokeObjectURL(blobUrl);
        };

        $scope.import = async function () {
            $scope.processing = true;
            
            const csvFile = document.getElementById("csvFile");
            const zipFile = document.getElementById("zipFile");

            if (!csvFile.value || csvFile.value.split(".")[1].toLowerCase() != "csv") {
                processingError("A valid CSV file must be specified");
                
                return;
            }

            if (zipFile.value && zipFile.value.split(".")[1].toLowerCase() != "zip") {
                processingError("The selected file is not a valid ZIP file");
                
                return;
            }

            const csvStorageToken = await getStorageToken(csvFile);
            const zipStorageToken = await getStorageToken(zipFile);

            let req = {
                datePattern: $scope.datePattern.id,
                moveUpdatedContentToCurrentLocation: $scope.moveUpdatedContentToCurrentLocation,
                csvFile: csvStorageToken,
                zipFile: zipStorageToken,
            };

            let result = await fetch(`/umbraco/backoffice/api/Imports/queue/${$scope.content.key}/${$scope.contentType.alias}`, {
                method: "POST",
                headers: {
                    "Accept": "*/*",
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(req)
            });

            if (result.status === 200) {
                $scope.show = "success";
                $scope.processing = false;
            } else {
                processingError(await result.json());
            }
            // $digest() is used to refresh the div contents. Use better alternative when possible. 
            $scope.$digest();
        };

        $scope.selectAllProperties = function () {
            for (let property of $scope.importableProperties) {
                property.selected = true;
            }
        }

        $scope.clearSelectedProperties = function () {
            for (let property of $scope.importableProperties) {
                property.selected = false;
            }
        }

        async function getStorageToken(input) {
            if (input.files.length === 0) {
                return null;
            }

            const data = new FormData();
            data.append("file", input.files[0]);

            var res = await fetch("/umbraco/api/Storage/tempUpload", {
                method: "POST",
                body: data
            });

            return await res.json();
        }

        function processingError(messages) {
            if (!Array.isArray(messages)) {
                messages = [ messages ];
            }

            $scope.processing = false;
            $scope.errorMessages = messages;
            $scope.show = "error";
        }

        async function getContentTypes(contentId) {
            const getContentType = await fetch(`/umbraco/backoffice/api/ContentTypes/${contentId}/relations?type=child`, {
                headers: {
                    "Accept": "application/json"
                }
            });

            return await getContentType.json();
        }

        assetsService.loadCss("~/App_Plugins/N3O.Umbraco.Data.Import/N3O.Umbraco.Data.Import.css");
    });
