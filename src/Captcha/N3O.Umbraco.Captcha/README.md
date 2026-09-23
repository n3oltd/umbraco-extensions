# N3O.Umbraco.Captcha

Validates Google reCAPTCHA tokens on incoming requests, supporting both v2 and v3. A request model
includes `CaptchaReq` and its validator rejects the request when the token does not verify, so
protecting an endpoint is a matter of the model rather than the controller.

Which version is in use is decided by content, not configuration: `ICaptchaValidatorFactory` returns
the first validator whose settings content exists, so a site enables v2 or v3 by creating the
corresponding settings node with its site and secret keys. Create neither and no validator is
returned; create both and which of the two is used is unspecified, so keep only the one in use. The
v3 settings also carry the score threshold below which a token is treated as failing.
