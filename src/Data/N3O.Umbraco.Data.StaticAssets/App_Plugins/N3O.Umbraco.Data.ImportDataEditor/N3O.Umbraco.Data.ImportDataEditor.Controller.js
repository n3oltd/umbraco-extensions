angular.module("umbraco").controller("N3O.Umbraco.Data.ImportDataEditor",
    function ($scope, assetsService) {
        $scope.uploadResource = async function (reference, index) {
            var uploadInput = document.getElementById("fileInput_" + index);
            
            if (uploadInput.files.length === 0) {
                return;
            }
            
            const storageToken = await getStorageToken(uploadInput);

            let req = {
                file: storageToken
            };

            let res = await fetch(`/umbraco/backoffice/api/Imports/queued/${reference}/files`, {
                method: "POST",
                headers: {
                    "Accept": "*/*",
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(req)
            });

            if (res.status === 200) {
                $scope.model.value.fields[index].value = uploadInput.files[0].name;
            } else {
                alert("Failed to upload specified file, please contact support for assistance");
            }
        }

        async function getStorageToken(uploadInput) {
            const data = new FormData();
            data.append("file", uploadInput.files[0]);

            var res = await fetch("/umbraco/backoffice/api/Storage/tempUpload", {
                method: "POST",
                body: data
            });

            return await res.json();
        }

        assetsService.loadCss("~/App_Plugins/N3O.Umbraco.Data.ImportDataEditor/N3O.Umbraco.Data.ImportDataEditor.css");
    });