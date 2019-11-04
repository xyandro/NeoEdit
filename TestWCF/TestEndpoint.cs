using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace TestWCF
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple, AddressFilterMode = AddressFilterMode.Any)]
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
	public class TestEndPoint : ITestEndPoint
	{
		object DefaultFor(Type type)
		{
			var underLying = Nullable.GetUnderlyingType(type);
			if (underLying != null)
				return DefaultFor(underLying);

			if (type == typeof(bool))
				return true;
			if (type == typeof(int))
				return 5;
			if (type == typeof(DateTime))
				return DateTime.Now;
			if (type == typeof(string))
				return "MyString";

			var result = Activator.CreateInstance(type);

			if ((type.IsGenericType) && ((type.GetGenericTypeDefinition() == typeof(List<>)) || (type.GetGenericTypeDefinition() == typeof(HashSet<>))))
			{
				var itemType = type.GetGenericArguments()[0];
				var addMethod = type.GetMethod("Add");
				for (var ctr = 0; ctr < 5; ++ctr)
					addMethod.Invoke(result, new object[] { DefaultFor(itemType) });
				return result;
			}


			var props = type.GetProperties();
			foreach (var prop in props)
				if (prop.CanWrite)
					prop.SetValue(result, DefaultFor(prop.PropertyType));

			return result;
		}

		T DefaultFor<T>() => (T)DefaultFor(typeof(T));

		public AMPAddressValidationReply AddressValidation(AMPAddressValidationRequest request) => DefaultFor<AMPAddressValidationReply>();
		public string AMPStatus() => DefaultFor<string>();
		public ClearCacheReply ClearCache() => DefaultFor<ClearCacheReply>();
		public ConfirmShipmentReply ConfirmShipment(ConfirmShipmentRequest request) => DefaultFor<ConfirmShipmentReply>();
		public GetAddressValidationReply GetAddressValidation(GetAddressValidationRequest request) => DefaultFor<GetAddressValidationReply>();
		public string GetAuthenticationSchemes() => DefaultFor<string>();
		public GetCarrierDeliveryDateReply GetCarrierDeliveryDate(GetCarrierDeliveryDateRequest request) => DefaultFor<GetCarrierDeliveryDateReply>();
		public GetDeliveredDateReply GetDeliveredDate(GetDeliveredDateRequest request) => DefaultFor<GetDeliveredDateReply>();
		public string GetEnvironment() => DefaultFor<string>();
		public GetOrderShipmentReply GetOrderShipment(GetOrderShipmentRequest request) => DefaultFor<GetOrderShipmentReply>();
		public AMPGetRatingReply GetRating(AMPGetRatingRequest request) => DefaultFor<AMPGetRatingReply>();
		public AMPShipmentReply GetShipment(AMPShipmentRequest request) => DefaultFor<AMPShipmentReply>();
		public GetShipMethodsReply GetShipMethods(GetShipMethodsRequest request) => DefaultFor<GetShipMethodsReply>();
		public string GetVersion() => DefaultFor<string>();
		public bool IsAlive() => DefaultFor<bool>();
		public AMPVoidShipmentReply VoidShipment(AMPVoidShipmentRequest request) => DefaultFor<AMPVoidShipmentReply>();
		public VoidUnshippedReply VoidUnshipped(VoidUnshippedRequest request) => DefaultFor<VoidUnshippedReply>();
		public GetAllTypesReply GetAllTypes() => new GetAllTypesReply();
		public GetAllTypesReply PassAllTypes(GetAllTypesReply reply) => reply;

		public string PassParams(int param1) => param1.ToString();
	}
}
