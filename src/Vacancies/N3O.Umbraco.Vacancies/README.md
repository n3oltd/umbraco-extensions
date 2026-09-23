# N3O.Umbraco.Vacancies

Content types and querying for job vacancies: a vacancies page, a container and the vacancies
themselves. `IVacanciesFinder` takes criteria and returns vacancies as whichever strongly-typed
content model the caller asks for, so a site can extend the vacancy type and still use the finder.

Vacancies are addressed by a URL that does not include the container node. A content finder resolves
those URLs and a URL provider generates them, so a vacancy keeps one address wherever it is linked
from.
