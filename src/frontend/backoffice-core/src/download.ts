import { blobDownload } from "@umbraco-cms/backoffice/utils";

export async function downloadResponse(
  response: Response,
  fallbackFilename: string,
): Promise<void> {
  const blob = await response.blob();
  const header = response.headers.get("Content-Disposition") ?? "";
  const parts = header.split(";");
  const filename =
    (parts[1] ?? "").split("=")[1]?.replaceAll('"', "") ?? fallbackFilename;

  blobDownload(blob, filename, blob.type);
}
