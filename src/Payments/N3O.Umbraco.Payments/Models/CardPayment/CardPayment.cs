using Newtonsoft.Json;
using System;

namespace N3O.Umbraco.Payments.Models;

public class CardPayment : Value {
    [JsonConstructor]
    public CardPayment(bool threeDSecureRequired,
                       bool threeDSecureCompleted,
                       ThreeDSecureV1 threeDSecureV1,
                       ThreeDSecureV2 threeDSecureV2) {
        ThreeDSecureRequired = threeDSecureRequired;
        ThreeDSecureCompleted = threeDSecureCompleted;
        ThreeDSecureV1 = threeDSecureV1;
        ThreeDSecureV2 = threeDSecureV2;
    }

    public CardPayment() : this(false, false, null, null) { }

    public bool ThreeDSecureRequired { get; }
    public bool ThreeDSecureCompleted { get; }
    public ThreeDSecureV1 ThreeDSecureV1 { get; }
    public ThreeDSecureV2 ThreeDSecureV2 { get; }

    public CardPayment ThreeDSecureComplete(string res) {
        if (!ThreeDSecureRequired || ThreeDSecureCompleted) {
            throw new InvalidOperationException();
        }

        var threeDSecureV1 = ThreeDSecureV1?.Complete(res);
        var threeDSecureV2 = ThreeDSecureV2?.Complete(res);
        
        return new CardPayment(true, true, threeDSecureV1, threeDSecureV2);
    }
}
