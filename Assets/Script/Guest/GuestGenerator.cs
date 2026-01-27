using System.Collections.Generic;
//Customer+CustomerRequest完整客人
public class GuestGenerator
{
    public List<Guest> BuildGuestsForToday(List<Customer> customers, IReadOnlyList<Item> playerData)
    {
        if (customers == null || customers.Count == 0) return new List<Guest>();

        var requestGenerator = new RequestGenerator();
        var requests = requestGenerator.GenerateRequests(customers, playerData ?? new List<Item>());

        var guests = new List<Guest>();
        for (int i = 0; i < customers.Count; i++)
        {
            var customer = customers[i];
            var request = i < requests.Count ? requests[i] : null;
            var guest = new Guest
            {
                customer = customer,
                request = request
            };
            guests.Add(guest);
        }
        return guests;
    }

}
public class Guest
{
    public Customer customer;
    public CustomerRequest request;

}