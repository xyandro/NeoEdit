using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TestWCF
{
	[DataContract]
	public class Reply
	{
		[DataMember]
		public string PartnerRequestReferenceNumber { get; set; }

		[DataMember]
		public bool Success { get; set; }
	}

	[DataContract]
	public class Request
	{
		[DataMember]
		public string PartnerRequestReferenceNumber { get; set; }

		[DataMember]
		public string CallingServiceName { get; set; }
	}

	[DataContract]
	public class Address
	{
		[DataMember]
		public string Address1 { get; set; }
		[DataMember]
		public string Address2 { get; set; }
		[DataMember]
		public string Address3 { get; set; }
		[DataMember]
		public string City { get; set; }
		[DataMember]
		public string CountryCode { get; set; }
		[DataMember]
		public string State { get; set; }
		[DataMember]
		public string ZipCode { get; set; }
		[DataMember]
		public bool Residential { get; set; }
		[DataMember]
		public bool IsPOBox { get; set; }
	}

	[DataContract]
	public class AMPAddressValidationReply : Reply
	{
		[DataMember]
		public string ServerAddress { get; set; }
		[DataMember]
		public List<Address> Addresses { get; set; }
	}

	[DataContract]
	public class AMPAddressValidationRequest : Request
	{
		[DataMember]
		public string Address1 { get; set; }
		[DataMember]
		public string Address2 { get; set; }
		[DataMember]
		public string Address3 { get; set; }
		[DataMember]
		public string City { get; set; }
		[DataMember]
		public string countryCode { get; set; }
		[DataMember]
		public string State { get; set; }
		[DataMember]
		public string ZipCode { get; set; }
	}

	[DataContract]
	public enum InternationalType
	{
		[EnumMember]
		Domestic,
		[EnumMember]
		UsTerritory,
		[EnumMember]
		Canadian,
		[EnumMember]
		International
	}

	[DataContract]
	public class RateItem
	{
		[DataMember]
		public bool IsUsable { get; set; }
		[DataMember]
		public string ShipMethod { get; set; }
		[DataMember]
		public int? TimeInTransit { get; set; }
		[DataMember]
		public decimal? Cost { get; set; }
		[DataMember]
		public bool RequiresKiosk { get; set; }
		[DataMember]
		public int Priority { get; set; }
		[DataMember]
		public string ErrorMessage { get; set; }

		//properties NOT exposed through interface
		public int ConnectShipErrorCode { get; set; }
		public string ConnectShipErrorMessage { get; set; }
		public string ConnectShipServiceCode { get; set; }
		public string Test { get; set; }

		public override string ToString()
		{
			return $"{Test}\t{ShipMethod}\t{ConnectShipServiceCode}\t{IsUsable}\t{ErrorMessage}\t{Cost}\t{TimeInTransit}\t{RequiresKiosk}\t{Priority}";
		}
	}

	[DataContract]
	public class AMPGetRatingReply : Reply
	{
		[DataMember]
		public InternationalType InternationalType { get; set; }
		[DataMember]
		public bool IsMilitary { get; set; }
		[DataMember]
		public bool IsPoBox { get; set; }
		[DataMember]
		public List<RateItem> Items { get; set; }
	}

	[DataContract]
	public class AddressRequest
	{
		[DataMember]
		public string CustomerName { get; set; }
		[DataMember]
		public string CustomerPhone { get; set; }
		[DataMember]
		public string Address1 { get; set; }
		[DataMember]
		public string Address2 { get; set; }
		[DataMember]
		public string City { get; set; }
		[DataMember]
		public string State { get; set; }
		[DataMember]
		public string Country { get; set; }
		[IgnoreDataMember]
		public string CountryCode { get; set; }
		[DataMember]
		public string PostalCode { get; set; }
		[DataMember]
		public bool IsCommercialAddress { get; set; }
	}

	[DataContract]
	public class AMPGetRatingRequest : Request
	{
		[DataMember]
		public string Account { get; set; }
		[DataMember]
		public int SiteID { get; set; }
		[DataMember]
		public DateTime ShipDate { get; set; }
		[DataMember]
		public string ServiceLevel { get; set; }
		[DataMember]
		public bool SignatureRequired { get; set; }
		[DataMember]
		public float Weight { get; set; }
		[DataMember]
		public AddressRequest ShippingAddress { get; set; }

		public string ToStr()
		{
			return $"{ShippingAddress.Address1}, {ShippingAddress.Address2}, {ShippingAddress.City}, {ShippingAddress.State}, {ShippingAddress.PostalCode}, SigReq: {SignatureRequired}";
		}
	}

	[DataContract]
	public class MAXICode
	{
		[DataMember]
		public string MaxiCodeAddressValidation { get; set; }

		[DataMember]
		public string MaxiCodeCountryCode { get; set; }

		[DataMember]
		public string MaxiCodeJulianPickupDay { get; set; }

		[DataMember]
		public string MaxiCodePackageNofX { get; set; }

		[DataMember]
		public string MaxiCodePackageWeight { get; set; }

		[DataMember]
		public string MaxiCodeSCAC { get; set; }

		[DataMember]
		public string MaxiCodeService { get; set; }

		[DataMember]
		public string MaxiCodeShipmentIdNumber { get; set; }

		[DataMember]
		public string MaxiCodeShipToAddress { get; set; }

		[DataMember]
		public string MaxiCodeShipToCity { get; set; }

		[DataMember]
		public string MaxiCodeShipToState { get; set; }

		[DataMember]
		public string MaxiCodeTrackingNumber { get; set; }

		[DataMember]
		public string MaxiCodeUpsShipperNumber { get; set; }
		[DataMember]
		public string MaxiCodeZip { get; set; }

	}

	[DataContract]
	public class DHL
	{

		[DataMember]
		public string PrimaryOutbound { get; set; }

		[DataMember]
		public string PrimaryInbound { get; set; }

		[DataMember]
		public string DestinationTerminal { get; set; }

		[DataMember]
		public string SortCodeVersion { get; set; }

		[DataMember]
		public string AdditionalEndorsement { get; set; }

		[DataMember]
		public string EndiciaLetters { get; set; }

		[DataMember]
		public string MailType { get; set; }

		[DataMember]
		public string USPSCarrierRoute { get; set; }

		[DataMember]
		public string ProductID { get; set; }

	}

	[DataContract]
	public class MailInnovations
	{
		[DataMember]
		public string DataMatrixBarcode { get; set; }
		[DataMember]
		public string CostCenter { get; set; }
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Contacts.ShippingCarrierService.Transport.Contracts")]
	public class AMPShipmentReply : Reply
	{
		[DataMember]
		public string OrderNo { get; set; }

		[DataMember]
		public string MSN { get; set; }

		[DataMember]
		public DateTime PickupDate { get; set; }

		[DataMember]
		public string PostalCode { get; set; }
		[DataMember]
		public string PostalCode128 { get; set; }

		[DataMember]
		public string RoutingCode { get; set; }

		[DataMember]
		public string ServerAddress { get; set; }

		[DataMember]
		public string ServiceIcon { get; set; }

		[DataMember]
		public string ServiceName { get; set; }

		[DataMember]
		public decimal Total { get; set; }

		[DataMember]
		public string TrackingNumber { get; set; }

		[DataMember]
		public string TrackingNumber128 { get; set; }

		[DataMember]
		public string PackageIdentificationCode { get; set; }

		[DataMember]
		public string BannerText { get; set; }

		[DataMember]
		public MAXICode MAXICodeData { get; set; }

		[DataMember]
		public string UpsIntlBackupDocContents { get; set; }

		[DataMember]
		public DHL DHLData { get; set; }

		[DataMember]
		public MailInnovations MailInnovationsData { get; set; }

		[DataMember]
		public string USPSPermitNo { get; set; }

		[DataMember]
		public string ReturnAddress1 { get; set; }
		[DataMember]
		public string ReturnAddress2 { get; set; }
		[DataMember]
		public string ReturnCity { get; set; }
		[DataMember]
		public string ReturnState { get; set; }
		[DataMember]
		public string ReturnPostalCode { get; set; }
		[DataMember]
		public string ReturnCountry { get; set; }

		[DataMember]
		public DateTime? DeliveryDateMin { get; set; }
		[DataMember]
		public DateTime? DeliveryDateMax { get; set; }
	}

	[DataContract]
	public enum CarrierEnum
	{
		[EnumMember]
		UnspecifiedCarrier = 0,
		[EnumMember]
		Dhl = 1,
		[EnumMember]
		Ups = 2,
		[EnumMember]
		Usps = 3,
		[EnumMember]
		MailInnov = 4
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Contacts.ShippingCarrierService.Transport.Contracts")]
	public enum SiteEnum
	{
		[EnumMember]
		Unknown = 0,
		[EnumMember]
		SaltLakeCity = 1,
		[EnumMember]
		Chicago = 2,
		[EnumMember]
		Charlotte = 3,
	}

	public class AddressTypeX
	{
		public InternationalType InternationalType { get; set; }
		public bool IsMilitary { get; set; }
		public bool IsPoAddress { get; set; }
	}

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Contacts.ShippingCarrierService.Transport.Contracts")]
	public class AMPShipmentRequest : Request
	{
		[DataMember]
		public string Account { get; set; }

		[DataMember]
		public string OrderNumber { get; set; }

		[DataMember]
		public float OrderWeight { get; set; }

		[Obsolete]
		[DataMember]
		public float OrderTotal { get; set; }

		[DataMember]
		public float DeclaredValue { get; set; }

		[DataMember]
		public int OrderQty { get; set; }

		[DataMember]
		public string CustomerName { get; set; }

		[DataMember]
		public string CustomerPhone { get; set; }

		[DataMember]
		public string Address1 { get; set; }

		[DataMember]
		public string Address2 { get; set; }

		[DataMember]
		public string City { get; set; }

		[DataMember]
		public string State { get; set; }

		[DataMember]
		public string Zip { get; set; }

		[DataMember]
		public string Country { get; set; }

		[IgnoreDataMember]
		public string CountryCode { get; set; }

		[DataMember]
		public bool SigRequired { get; set; }

		[DataMember]
		public string ServiceLevel { get; set; }

		[DataMember]
		public string ShipMethod { get; set; }

		[IgnoreDataMember]
		public CarrierEnum Carrier { get; set; }

		[DataMember]
		public DateTime ShipDate { get; set; }

		[DataMember]
		public bool IsCommercialAddress { get; set; }

		[DataMember]
		public bool IsReturnLabel { get; set; }

		[DataMember]
		public string SystemName { get; set; }

		[DataMember(IsRequired = false)]
		public string ManifestReference { get; set; }

		/// <summary>
		/// Although the ship operation is used to save transactions for later processing, the saveTransaction
		/// flag may be used to override this behavior. If saveTransaction is set to false, the operation will 
		/// perform all validations and calculations, but will not save the data. Those unsaved packages will 
		/// not be able to have documents produced for them, nor will they be manifested during end-of-day 
		/// processing. (i.e. set to false for testing)
		/// </summary>
		[DataMember]
		public bool SaveTransaction { get; set; }

		[DataMember(IsRequired = false)]
		public SiteEnum Site { get; set; }

		[DataMember(IsRequired = false)]
		public int SiteID { get; set; }

		[DataMember(IsRequired = false)]
		public string BoxDimensions { get; set; }

		[IgnoreDataMember]
		public AddressTypeX AddressType { get; set; }
	}

	[DataContract]
	public class AMPVoidShipmentReply : Reply
	{

	}

	[DataContract]
	public class AMPVoidShipmentRequest : Request
	{
		[DataMember]
		public string TrackingNumber { get; set; }

		[DataMember]
		public string OrderNumber { get; set; }

		[DataMember(IsRequired = false)]
		public CarrierEnum Carrier { get; set; }

		//[DataMember(IsRequired = false)]
		//public SiteEnum Site { get; set; }
	}

	[DataContract]
	public class ClearCacheReply : Reply
	{
	}

	[DataContract]
	public class ConfirmShipmentReply : Reply
	{
	}

	[DataContract]
	public class ConfirmShipmentRequest
	{
		[DataMember]
		public string OrderNo { get; set; }

		[DataMember]
		public float PackageWeightActual { get; set; }

		[DataMember]
		public DateTime ShipDate { get; set; }
	}

	[DataContract]
	public class ValidatedAddress
	{
		[DataMember]
		public string FirmName { get; set; }

		[DataMember]
		public string Address1 { get; set; }

		[DataMember]
		public string Address2 { get; set; }
		[DataMember]
		public string Address3 { get; set; }

		[DataMember]
		public string City { get; set; }

		[DataMember]
		public string State { get; set; }

		[DataMember]
		public string Zip5 { get; set; }

		[DataMember]
		public string Zip4 { get; set; }

		[DataMember]
		public string Urbanization { get; set; }

		[DataMember]
		public string DeliveryPoint { get; set; }

		[DataMember]
		public string CarrierRoute { get; set; }
	}

	[DataContract]
	public class GetAddressValidationReply : Reply
	{
		[DataMember]
		public List<ValidatedAddress> ValidatedAddresses { get; set; }
	}

	[DataContract]
	public class ValidationAddress
	{
		[DataMember]
		public string FirmName { get; set; }

		[DataMember]
		public string Address1 { get; set; }

		[DataMember]
		public string Address2 { get; set; }

		[DataMember]
		public string City { get; set; }

		[DataMember]
		public string State { get; set; }

		[DataMember]
		public string Zip5 { get; set; }

		[DataMember]
		public string Urbanization { get; set; }
	}

	[DataContract]
	public class GetAddressValidationRequest
	{
		[DataMember]
		public bool IncludeOptionalElements { get; set; }

		[DataMember]
		public bool ReturnCarrierRoute { get; set; }

		[DataMember]
		public List<ValidationAddress> Addresses { get; set; }
	}

	[DataContract]
	public class GetCarrierDeliveryDateReply : Reply
	{
		[DataMember]
		public DateTime CarrierDeliveryDate { get; set; }
	}

	public enum ShipDestinationTypeEnum { Residential, Commercial }
	public enum CompanyTypeEnum { Contacts, Glasses }

	[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Contacts.ShippingCarrierService.Transport.Contracts")]
	public class ShipRequest : Request
	{
		[DataMember]
		public string Street1 { get; set; }

		[DataMember]
		public string Street2 { get; set; }

		[DataMember]
		public string City { get; set; }

		[DataMember]
		public string StateProvince { get; set; }

		[DataMember]
		public string ZipPostalCode { get; set; }

		[DataMember]
		public string Country { get; set; }

		[DataMember]
		public string CountryCode { get; set; }

		[DataMember]
		public DateTime ShipDate { get; set; }

		[DataMember]
		public decimal WeightInLbs { get; set; }

		[DataMember]
		public decimal OrderTotal { get; set; }

		[DataMember]
		public string RecipientPhoneNumber { get; set; }

		[DataMember]
		public bool UseSignatureService { get; set; }

		[DataMember]
		public string ShipperFirstName { get; set; }

		[DataMember]
		public string ShipperLastName { get; set; }

		[DataMember]
		public string ShipperCompanyName { get; set; }

		[DataMember]
		public string ShipperCompanyPhone { get; set; }

		[DataMember]
		public string ShipperStreet1 { get; set; }

		[DataMember]
		public string ShipperStreet2 { get; set; }

		[DataMember]
		public string ShipperCity { get; set; }

		[DataMember]
		public string ShipperState { get; set; }

		[DataMember]
		public string ShipperPostalCode { get; set; }

		[DataMember]
		public string ShipperCountry { get; set; }

		[DataMember]
		public string ShipperEmail { get; set; }

		[DataMember]
		public string CustomerFirstName { get; set; }

		[DataMember]
		public string CustomerLastName { get; set; }

		[DataMember]
		public string CustomerPhone { get; set; }

		[DataMember]
		public string OrderNo { get; set; }

		[DataMember]
		public string ShipCode { get; set; }

		[DataMember]
		public ShipDestinationTypeEnum ShipDestinationType { get; set; }

		[DataMember]
		public CompanyTypeEnum CompanyType { get; set; }
	}

	[DataContract]
	public class GetCarrierDeliveryDateRequest : Request
	{
		[DataMember]
		public ShipRequest ShipRequest { get; set; }
	}

	[DataContract]
	public class DeliveryEvent
	{
		[DataMember]
		public DateTime DateTime { get; set; }

		[DataMember]
		public string Location { get; set; }

		[DataMember]
		public string Description { get; set; }
	}

	[DataContract]
	public class GetDeliveredDateReply : Reply
	{
		[DataMember]
		public DateTime? DeliveredDate { get; set; }

		[DataMember]
		public DateTime? ExpectedDeliveryDate { get; set; }

		[DataMember]
		public bool DeliveredToUsps { get; set; }

		[DataMember]
		public List<string> DeliveryEvents { get; set; }

		[DataMember]
		public List<DeliveryEvent> DetailedDeliveryEvents { get; set; }

		[DataMember]
		public CarrierEnum Carrier { get; set; }

		[DataMember]
		public string TrackingURL { get; set; }
	}

	[DataContract]
	public class GetDeliveredDateRequest
	{
		[DataMember]
		public string TrackingNumber { get; set; }
	}

	[DataContract]
	public class Shipment
	{
		[DataMember]
		public string Trackingnumber { get; set; }
		[DataMember]
		public int OrderHeaderID { get; set; }
		[DataMember]
		public string TrackingURL { get; set; }
		[DataMember]
		public string Carrier { get; set; }
		[DataMember]
		public string ShipMethod { get; set; }
		[DataMember]
		public string ServiceLevel { get; set; }
	}

	[DataContract]
	public class GetOrderShipmentReply : Reply
	{
		[DataMember]
		public List<Shipment> Shipments { get; set; }
	}

	[DataContract]
	public class GetOrderShipmentRequest : Request
	{
		[DataMember]
		public string PartnerOrderNumber { get; set; }
		[DataMember]
		public int ChannelID { get; set; }
	}

	[DataContract]
	public class FulfillmentShipMethod
	{
		[DataMember]
		public string ShipMethodCode { get; set; }
		[DataMember]
		public string CustomerPriority { get; set; }
		[DataMember]
		public string Carrier { get; set; }
		[DataMember]
		public int Days { get; set; }
		[DataMember]
		public string TrackingUrlBase { get; set; }
		[DataMember]
		public string CustomerDescription { get; set; }
		[DataMember]
		public bool IsActive { get; set; }
	}

	[DataContract]
	public class GetShipMethodsReply : Reply
	{
		[DataMember]
		public List<FulfillmentShipMethod> ShipMethods { get; set; }
	}

	[DataContract]
	public class GetShipMethodsRequest : Request
	{
		[DataMember] public bool IncludeNotActive { get; set; } = false;
	}

	[DataContract]
	public class VoidUnshippedReply : Reply
	{
		[DataMember]
		public int VoidedCount { get; set; }
	}

	[DataContract]
	public class VoidUnshippedRequest : Request
	{
		[DataMember]
		public CarrierEnum Carrier { get; set; }

		[DataMember]
		public DateTime LastVoidDateTime { get; set; }

		[DataMember(IsRequired = false)]
		public SiteEnum Site { get; set; }

		[DataMember(IsRequired = false)]
		public int SiteID { get; set; }
	}
}
