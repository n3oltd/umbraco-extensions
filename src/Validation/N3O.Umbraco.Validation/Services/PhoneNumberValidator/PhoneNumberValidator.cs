using N3O.Umbraco.Lookups;
using PhoneNumbers;

namespace N3O.Umbraco.Validation;

public class PhoneNumberValidator : IPhoneNumberValidator {
    public bool IsValid(string number, Country country) {
        var phoneNumberUtil = PhoneNumberUtil.GetInstance();

        try {
            var phoneNumber = phoneNumberUtil.Parse(number, country.Iso2Code);
            var isValid = phoneNumberUtil.IsValidNumber(phoneNumber);

            return isValid;
        } catch (NumberParseException) {
            return false;
        }
    }
}
