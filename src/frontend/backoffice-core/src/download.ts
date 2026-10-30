export async function downloadResponse(
  response: Response,
  fallbackFilename: string,
): Promise<void> {
  const blob = await response.blob();
  const header = response.headers.get("Content-Disposition") ?? "";
  const parts = header.split(";");
  const filename =
    (parts[1] ?? "").split("=")[1]?.replaceAll('"', "") ?? fallbackFilename;
  const blobUrl = window.URL.createObjectURL(new Blob([blob]));
  const link = document.createElement("a");

  link.href = blobUrl;
  link.setAttribute("download", filename);
  // Umbraco's router turns a click on a same-origin link into a history push,
  // and a blob: URL is same-origin
  link.dataset.routerSlot = "disabled";

  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(blobUrl);
}
