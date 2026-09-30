using N3O.Umbraco.Localization;
using System.Net;

namespace N3O.Umbraco.Exceptions;

public class ResourceNotFoundException : ExceptionWithProblemDetails {
    public ResourceNotFoundException() { }

    public ResourceNotFoundException(string parameterName, string parameterValue) {
        ParameterName = parameterName;
        ParameterValue = parameterValue;
    }

    public string ParameterName { get; }
    public string ParameterValue { get; }
    public string Resource => $"{ParameterName}:{ParameterValue}";

    public override ProblemDetails GetProblemDetails(IFormatter formatter) {
        var problemDetails = new ProblemDetails(HttpStatusCode.NotFound, ParameterName, ParameterValue);

        return problemDetails;
    }
}
