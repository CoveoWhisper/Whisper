![development branch](https://travis-ci.org/CoveoWhisper/WhisperAPI.svg?branch=development)

<img src="https://i.imgur.com/CJq5KRm.png" width="400" align="right">

## Setup
### 1) Download Repositories
The following repositories need to be downloaded:

* Whisper Api: <https://github.com/CoveoWhisper/WhisperAPI>
* NLP Api: <https://github.com/CoveoWhisper/NLPAPI>
* ML Api: <https://github.com/CoveoWhisper/MLAPI>

### 2) Setup NLP API
1. Create a Dialogflow agent: <https://dialogflow.com/>
2. Generate `dialogflow_secret.json` (see also: <https://dialogflow.com/docs/reference/v2-auth-setup>)
![picture alt](https://i.imgur.com/Wcff3l5.png)
    To generate the file, go on the Google Cloud Platform/Service Accounts page for the project:              <https://console.cloud.google.com/iam-admin/serviceaccounts>. Then Create Key and choose the json format. Then rename the file as   dialogflow_secret.json and copy to the /api directory
3. Generate the  `query_model.bin` file and paste in the /api directory (see **Generating Models**)
4. Set port in  _index.py_
<img src="https://i.imgur.com/6MGEx1t.png" width="400" align="center">

5. In a command line prompt run the following command: `pip install -r requirements.txt`

### 3) Setup ML API
1. Generate `facets.bin` and copy to root folder (see **Generating Models**)
2. Generate `documents_popularity.json` and copy to root folder (see **Generating Models**)
3. Generate `documents_searches__mapping.json` and copy to root folder (see **Generating Models**)
4. Set port in _app.py_
<img src="https://i.imgur.com/ABTdoVr.png" width="400" align="center">
5. In a command line prompt run the following command: `pip install -r requirements.txt`

### 4 ) Setup Whisper API
1. Create `appsettings_secret.json` file at the same level as appsettings.json with the following content:
		
```Perl
{
  "ApiKey": "yy50124000-aaaa-8888-dddd-0003dc9ec111",
  "NlpApiBaseAddress": "http://localhost:5000/", 
  "MlApiBaseAddress": "http://localhost:5001/",
  "SearchBaseAddress": "https://platform.cloud.coveo.com/",
  "OrganizationId": "orgId2940293"
}
```

* ApiKey: should be set to the Coveo Search Api key.
* NlpApiBaseAddress and MlApiBaseAddress: should be set to api addresses from 2.4) and 3.4)
* OrganizationId: should be set to the Coveo organization ID.

2. The app runs on <http://localhost:52256/>

### 5) Swagger documentation:
<http://localhost:52256/swagger/index.html/>
	
é
