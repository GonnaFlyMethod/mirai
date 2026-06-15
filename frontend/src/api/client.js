export async function api(path, options = {}) {
  const response = await fetch(path, options);

  if (options.empty && response.ok) {
    return null;
  }

  const contentType = response.headers.get("content-type") || "";
  const body = contentType.includes("application/json") ? await response.json() : await response.text();

  if (!response.ok) {
    throw new Error(body?.error || body || `Request failed with ${response.status}`);
  }

  return body;
}
