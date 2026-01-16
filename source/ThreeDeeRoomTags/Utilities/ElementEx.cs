namespace ThreeDeeRoomTags.Utilities
{
    public static class ElementEx
    {
        public static bool IsElementEditable(this Element element)
        {
            var doc = element.Document;

            if (!doc.IsWorkshared) return true;

            var checkoutStatus = WorksharingUtils.GetCheckoutStatus(doc, element.Id);

            if (checkoutStatus == CheckoutStatus.OwnedByOtherUser) return false;

            var modelUpdateStatus = WorksharingUtils.GetModelUpdatesStatus(doc, element.Id);

            if(modelUpdateStatus == ModelUpdatesStatus.DeletedInCentral || modelUpdateStatus == ModelUpdatesStatus.UpdatedInCentral) return false;
            
            return true;
        }
    }
}
