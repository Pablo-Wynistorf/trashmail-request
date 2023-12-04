
# Trashmail Request

This simple application is made for creatig temporary email addresses using the cloudflare api. It works like so: 

1.) The app generates a random email address. 

2.) Then it does a api request to cloudflare.

3.) Cloudflare makes the email route entry on your domain. 

4.) You can delete the temporary email with one click.



If you want to use this application, you just need to download the app directory from my git repo. Then you need to add the missing parts into the config.json file. It should look something like this:


```
{
    "zoneId": "email_domain_zone_id",
    "apiKey": "cloudflare_api_key",
    "authEmail": "cloudflare@email.com",
    "emailDomain": "example.com",
    "forwardAddress": "example@email.com"
}
```

But before you test it, you need to make sure you have everything configured correctly, on the cloudflare site. 

1.) You need to have email routing enabled on your domain. 

2.) Your "forwardAddress" needs to be verified in the cloudflare dashboard. 

3.) The cloudflare apiKey needs to have enough permissions, to create, delete and edit email routes.
