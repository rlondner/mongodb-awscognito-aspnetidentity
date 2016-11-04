using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace IdentitySample.Controllers
{
    public class FilesController : Controller
    {
        internal const string DEVELOPER_PROVIDER_NAME = "aspnet.identity.mongodb.apps.S3Demo";
        string strWorkerBeeCognitoPoolId = string.Empty;
        string strQueenBeeCognitoPoolId = string.Empty;
        string strBeeHiveCognitoPoolId = string.Empty;
        string strFreeBeeCognitoPoolId = string.Empty;

        string strUserCognitoPoolId = string.Empty;
        string strAccessKeyId = string.Empty;
        string strAccessKeySecret = string.Empty;

        string strCurrentUsername = string.Empty;

        public FilesController()
        {
            strWorkerBeeCognitoPoolId = ConfigurationManager.AppSettings["WorkerBeeCognitoPoolId"];
            strQueenBeeCognitoPoolId = ConfigurationManager.AppSettings["QueenBeeCognitoPoolId"];
            strFreeBeeCognitoPoolId = ConfigurationManager.AppSettings["FreeBeeCognitoPoolId"];
            strBeeHiveCognitoPoolId = ConfigurationManager.AppSettings["BeeHiveCognitoPoolId"];
            strAccessKeyId = ConfigurationManager.AppSettings["CognitoDeveloperAccessKeyId"];
            strAccessKeySecret = ConfigurationManager.AppSettings["CognitoDeveloperAccessKeySecret"];

        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Browse()
        {

            List<Models.S3File> files = new List<Models.S3File>();

            strCurrentUsername = HttpContext.User.Identity.Name;

            if (!string.IsNullOrEmpty(strCurrentUsername))
            {
                if (strCurrentUsername.EndsWith("workerbee.com"))
                {
                    strUserCognitoPoolId = strWorkerBeeCognitoPoolId;
                }
                else if (strCurrentUsername.EndsWith("queenbee.com"))
                {
                    strUserCognitoPoolId = strQueenBeeCognitoPoolId;
                }
                else if (strCurrentUsername.EndsWith("beehive.com"))
                {
                    strUserCognitoPoolId = strBeeHiveCognitoPoolId;
                }
                else //any other email domain
                {
                    strUserCognitoPoolId = strFreeBeeCognitoPoolId;
                }
            }

            Amazon.RegionEndpoint northVirginiaRegion = Amazon.RegionEndpoint.USEast1; //Virginia location


            string strAccessKeyId = ConfigurationManager.AppSettings["CognitoDeveloperAccessKeyId"];
            string strAccessKeySecret = ConfigurationManager.AppSettings["CognitoDeveloperAccessKeySecret"];


            AmazonCognitoIdentityClient cognitoIdClient = new AmazonCognitoIdentityClient(strAccessKeyId, strAccessKeySecret, northVirginiaRegion);

            if (cognitoIdClient != null)
            {
                Dictionary<string, string> customLogin = new Dictionary<string, string>();
                customLogin.Add(DEVELOPER_PROVIDER_NAME, HttpContext.User.Identity.Name);
                GetOpenIdTokenForDeveloperIdentityRequest oidcTokenReq = new GetOpenIdTokenForDeveloperIdentityRequest();
                //oidcTokenReq.IdentityId = HttpContext.User.Identity.Name;
                oidcTokenReq.IdentityPoolId = strUserCognitoPoolId;
                oidcTokenReq.TokenDuration = 86400; //24hr for ID token validaty
                oidcTokenReq.Logins = customLogin;


                //Get an OpenID Connect token from AWS Cognito
                GetOpenIdTokenForDeveloperIdentityResponse oidcTokenRes = cognitoIdClient.GetOpenIdTokenForDeveloperIdentity(oidcTokenReq);

                //Get the Cognito Identity ID for the current user
                GetCredentialsForIdentityRequest credentialsForIdReq = new GetCredentialsForIdentityRequest()
                {
                    IdentityId = oidcTokenRes.IdentityId,
                    //Logins = customLogin
                };


                Dictionary<string, string> token = new Dictionary<string, string>();
                token.Add("cognito-identity.amazonaws.com",oidcTokenRes.Token);//

                //Get the token from AWS STS (through AWS Cognito) to assume an AWS role that will allow to user to query AWS S3
                GetCredentialsForIdentityResponse credentialsForIdRes =                     cognitoIdClient.GetCredentialsForIdentity(oidcTokenRes.IdentityId, token );
                Credentials awsCreds = credentialsForIdRes.Credentials;

                using (var s3Client = new AmazonS3Client(awsCreds, northVirginiaRegion))
                {
                    try
                    {
                        var bucketsRes = s3Client.ListBuckets();
                        List<S3Bucket> buckets = bucketsRes.Buckets;

                        foreach (S3Bucket bucket in buckets)
                        {
                            try
                            {
                                ListObjectsResponse listObjectsRes = s3Client.ListObjects(bucket.BucketName);
                                List<S3Object> s3Objects = listObjectsRes.S3Objects;
                                foreach(S3Object s3file in s3Objects)
                                {
                                    files.Add(new Models.S3File() { FileName = s3file.Key });
                                    
                                }
                            }
                            catch (Exception ex)
                            {
                                
                            }
                        }
                    }
                    catch (AmazonCognitoIdentityException cex)
                    {
                        string strError = cex.ToString();
                    }
                    catch (Exception ex)
                    {

                        ////throw;
                    }
                }

            }

            return View(files);
        }
    }
}