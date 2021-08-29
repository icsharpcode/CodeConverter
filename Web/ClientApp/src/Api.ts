import axios from "axios";
import ClientSettings from "./ClientSettings.json"

const getBaseUriBySourceKey = (sourceKey: string | null | undefined) =>
    ClientSettings.baseUris.filter(b => b.key === sourceKey)[0];

const getUri = (relativeUri: string) => {
    const urlSearchParams = new URLSearchParams(window.location.search);
    const urlSourceKey = urlSearchParams.get("apiSource");
    const baseUri = (getBaseUriBySourceKey(urlSourceKey) ?? getBaseUriBySourceKey(document.head.dataset["apisource"])).baseUri;
    return baseUri + relativeUri;
};

export const getVersion = () => axios.get(getUri(ClientSettings.endpoints.version));

export const convert = (inputCode: string, conversionType: string) =>
    axios.post(getUri(ClientSettings.endpoints.convert), {
        code: inputCode,
        requestedConversion: conversionType
    });