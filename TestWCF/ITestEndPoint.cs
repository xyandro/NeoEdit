using System.ServiceModel;

namespace TestWCF
{
	[ServiceContract(Namespace = "http://www.1800contacts.com/ServiceContractTypes/1")]
	public interface ITestEndPoint
	{
		//[OperationContract]
		//bool IsAlive();

		//[OperationContract]
		//string GetVersion();

		//[OperationContract(IsOneWay = false)]
		//string GetEnvironment();

		//[OperationContract(IsOneWay = false)]
		//AMPShipmentReply GetShipment(AMPShipmentRequest request);

		//[OperationContract(IsOneWay = false)]
		//AMPVoidShipmentReply VoidShipment(AMPVoidShipmentRequest request);

		//[OperationContract(IsOneWay = false)]
		//string AMPStatus();

		//[OperationContract(IsOneWay = false)]
		//GetShipMethodsReply GetShipMethods(GetShipMethodsRequest request);

		//[OperationContract(IsOneWay = false)]
		//string GetAuthenticationSchemes();

		//[OperationContract(IsOneWay = false)]
		//GetCarrierDeliveryDateReply GetCarrierDeliveryDate(GetCarrierDeliveryDateRequest request);

		//[OperationContract(IsOneWay = false)]
		//GetDeliveredDateReply GetDeliveredDate(GetDeliveredDateRequest request);

		//[OperationContract(IsOneWay = false)]
		//GetAddressValidationReply GetAddressValidation(GetAddressValidationRequest request);

		//[OperationContract(IsOneWay = false)]
		//VoidUnshippedReply VoidUnshipped(VoidUnshippedRequest request);

		//[OperationContract(IsOneWay = false)]
		//AMPAddressValidationReply AddressValidation(AMPAddressValidationRequest request);

		//[OperationContract(IsOneWay = false)]
		//ClearCacheReply ClearCache();

		//[OperationContract(IsOneWay = false)]
		//ConfirmShipmentReply ConfirmShipment(ConfirmShipmentRequest request);

		//[OperationContract(IsOneWay = false)]
		//AMPGetRatingReply GetRating(AMPGetRatingRequest request);

		//[OperationContract(IsOneWay = false)]
		//GetOrderShipmentReply GetOrderShipment(GetOrderShipmentRequest request);

		[OperationContract(IsOneWay = false)]
		GetAllTypesReply GetAllTypes();

		[OperationContract(IsOneWay = false)]
		GetAllTypesReply PassAllTypes(GetAllTypesReply request);

		//[OperationContract(IsOneWay = false)]
		//string PassParams(int param1);
	}
}
