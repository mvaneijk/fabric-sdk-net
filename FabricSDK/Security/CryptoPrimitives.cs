/*
 *  Copyright 2016, 2017 DTCC, Fujitsu Australia Software Technology, IBM - All Rights Reserved.
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *        http://www.apache.org/licenses/LICENSE-2.0
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Hyperledger.Fabric.SDK.Exceptions;
using Hyperledger.Fabric.SDK.Helper;
using Hyperledger.Fabric.SDK.Logging;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.Utilities.IO.Pem;
using CryptoException = Hyperledger.Fabric.SDK.Exceptions.CryptoException;
using HashAlgorithm = System.Security.Cryptography.HashAlgorithm;
using PemReader = Org.BouncyCastle.OpenSsl.PemReader;

namespace Hyperledger.Fabric.SDK.Security
{
/*
    package org.hyperledger.fabric.sdk.security;

import java.io.BufferedInputStream;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.IOException;
import java.io.StringReader;
import java.io.StringWriter;
import java.math.BigInteger;
import java.security.InvalidAlgorithmParameterException;
import java.security.InvalidKeyException;
import java.security.KeyPair;
import java.security.KeyPairGenerator;
import java.security.KeyStore;
import java.security.KeyStoreException;
import java.security.NoSuchAlgorithmException;
import java.security.PrivateKey;
import java.security.Provider;
import java.security.SecureRandom;
import java.security.Security;
import java.security.Signature;
import java.security.SignatureException;
import java.security.cert.CertPath;
import java.security.cert.CertPathValidator;
import java.security.cert.CertPathValidatorException;
import java.security.cert.Certificate;
import java.security.cert.CertificateException;
import java.security.cert.CertificateFactory;
import java.security.cert.PKIXParameters;
import java.security.cert.X509Certificate;
import java.security.interfaces.ECPrivateKey;
import java.security.spec.ECGenParameterSpec;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Properties;
import java.util.concurrent.atomic.AtomicBoolean;

import javax.security.auth.x500.X500Principal;
import javax.xml.bind.DatatypeConverter;

import org.apache.commons.io.FileUtils;
import org.apache.commons.logging.Log;
import org.apache.commons.logging.LogFactory;
import org.bouncycastle.asn1.ASN1Encodable;
import org.bouncycastle.asn1.ASN1InputStream;
import org.bouncycastle.asn1.ASN1Integer;
import org.bouncycastle.asn1.ASN1Primitive;
import org.bouncycastle.asn1.ASN1Sequence;
import org.bouncycastle.asn1.DERSequenceGenerator;
import org.bouncycastle.asn1.pkcs.PrivateKeyInfo;
import org.bouncycastle.asn1.x9.ECNamedCurveTable;
import org.bouncycastle.asn1.x9.X9ECParameters;
import org.bouncycastle.crypto.Digest;
import org.bouncycastle.crypto.digests.SHA256Digest;
import org.bouncycastle.crypto.digests.SHA3Digest;
import org.bouncycastle.jce.provider.BouncyCastleProvider;
import org.bouncycastle.openssl.PEMKeyPair;
import org.bouncycastle.openssl.PEMParser;
import org.bouncycastle.openssl.jcajce.JcaPEMKeyConverter;
import org.bouncycastle.openssl.jcajce.JcaPEMWriter;
import org.bouncycastle.operator.ContentSigner;
import org.bouncycastle.operator.OperatorCreationException;
import org.bouncycastle.operator.jcajce.JcaContentSignerBuilder;
import org.bouncycastle.pkcs.PKCS10CertificationRequest;
import org.bouncycastle.pkcs.PKCS10CertificationRequestBuilder;
import org.bouncycastle.pkcs.jcajce.JcaPKCS10CertificationRequestBuilder;
import org.bouncycastle.util.io.pem.PemObject;
import org.bouncycastle.util.io.pem.PemReader;
import org.hyperledger.fabric.sdk.exception.CryptoException;
import org.hyperledger.fabric.sdk.exception.InvalidArgumentException;
import org.hyperledger.fabric.sdk.helper.Config;
import org.hyperledger.fabric.sdk.helper.DiagnosticFileDumper;

import static java.lang.String.format;
import static org.hyperledger.fabric.sdk.helper.Utils.isNullOrEmpty;
*/
    public class CryptoPrimitives : ICryptoSuite
    {
        private static readonly ILog logger = LogProvider.GetLogger(typeof(CryptoPrimitives));
        private static readonly Config config = Config.GetConfig();
        private static readonly bool IS_TRACE_LEVEL = logger.IsTraceEnabled();

        private static readonly DiagnosticFileDumper diagnosticFileDumper = IS_TRACE_LEVEL ? config.GetDiagnosticFileDumper() : null;

        private string curveName;
        private string hashAlgorithm = config.GetHashAlgorithm();
        private int securityLevel = config.GetSecurityLevel();
        private string CERTIFICATE_FORMAT = config.GetCertificateFormat();
        private string DEFAULT_SIGNATURE_ALGORITHM = config.GetSignatureAlgorithm();

        private Dictionary<int, string> securityCurveMapping = config.GetSecurityCurveMapping();

        // Following configuration settings are hardcoded as they don't deal with any interactions with Fabric MSP and BCCSP components
        // If you wish to make these customizable, follow the logic from setProperties();
        //TODO May need this for TCERTS ?
//    private String ASYMMETRIC_KEY_TYPE = "EC";
//    private String KEY_AGREEMENT_ALGORITHM = "ECDH";
//    private String SYMMETRIC_KEY_TYPE = "AES";
//    private int SYMMETRIC_KEY_BYTE_COUNT = 32;
//    private String SYMMETRIC_ALGORITHM = "AES/CFB/NoPadding";
//    private int MAC_KEY_BYTE_COUNT = 32;

        public CryptoPrimitives()
        {
            //String securityProviderClassName = config.getSecurityProviderClassName();

            //SECURITY_PROVIDER = setUpExplicitProvider(securityProviderClassName);

            //Decided TO NOT do this as it can have affects over the whole JVM and could have
            // unexpected results.  The embedding application can easily do this!
            // Leaving this here as a warning.
            // Security.insertProviderAt(SECURITY_PROVIDER, 1); // 1 is top not 0 :)
        }
        /*
        public Provider setUpExplicitProvider(String securityProviderClassName) throws InstantiationException, ClassNotFoundException, IllegalAccessException {
            if (null == securityProviderClassName)
            {
                throw new InstantiationException(format("Security provider class name property (%s) set to null  ", Config.SECURITY_PROVIDER_CLASS_NAME));
            }

            if (CryptoSuiteFactory.DEFAULT_JDK_PROVIDER.equals(securityProviderClassName))
            {
                return null;
            }

            Class < ?> aClass = Class.forName(securityProviderClassName);
            if (null == aClass)
            {
                throw new InstantiationException(format("Getting class for security provider %s returned null  ", securityProviderClassName));
            }

            if (!Provider.class.isAssignableFrom(aClass)) {
                throw new InstantiationException(format("Class for security provider %s is not a Java security provider", aClass.getName()));
            }
            Provider securityProvider = (Provider) aClass.newInstance();
            if (securityProvider == null)
            {
                throw new InstantiationException(format("Creating instance of security %s returned null  ", aClass.getName()));
            }

            return securityProvider;
        }
        */

//    /**
//     * sets the signature algorithm used for signing/verifying.
//     *
//     * @param sigAlg the name of the signature algorithm. See the list of valid names in the JCA Standard Algorithm Name documentation
//     */
//    public void setSignatureAlgorithm(String sigAlg) {
//        this.DEFAULT_SIGNATURE_ALGORITHM = sigAlg;
//    }

//    /**
//     * returns the signature algorithm used by this instance of CryptoPrimitives.
//     * Note that fabric and fabric-ca have not yet standardized on which algorithms are supported.
//     * While that plays out, CryptoPrimitives will try the algorithm specified in the certificate and
//     * the default SHA256withECDSA that's currently hardcoded for fabric and fabric-ca
//     *
//     * @return the name of the signature algorithm
//     */
//    public String getSignatureAlgorithm() {
//        return this.DEFAULT_SIGNATURE_ALGORITHM;
//    }

        public X509Certificate2 BytesToCertificate(byte[] certBytes)
        {
            if (certBytes == null || certBytes.Length == 0)
            {
                throw new CryptoException("bytesToCertificate: input null or zero length");
            }

            return GetX509Certificate(certBytes);


//        X509Certificate certificate;
//        try {
//            BufferedInputStream pem = new BufferedInputStream(new ByteArrayInputStream(certBytes));
//            CertificateFactory certFactory = CertificateFactory.getInstance(CERTIFICATE_FORMAT);
//            certificate = (X509Certificate) certFactory.generateCertificate(pem);
//        } catch (CertificateException e) {
//            String emsg = "Unable to converts byte array to certificate. error : " + e.getMessage();
//            logger.error(emsg);
//            logger.debug("input bytes array :" + new String(certBytes));
//            throw new CryptoException(emsg, e);
//        }
//
//        return certificate;
        }

        /**
         * Return X509Certificate  from pem bytes.
         * So you may ask why this ?  Well some providers (BC) seems to have problems with creating the
         * X509 cert from bytes so here we go through all available providers till one can convert. :)
         *
         * @param pemCertificate
         * @return
         */

        private X509Certificate2 GetX509Certificate(byte[] pemCertificate)
        {
            X509Certificate2 ret = null;
            CryptoException rete = null;
            Pkcs12Store store = new Pkcs12StoreBuilder().Build();
            X509CertificateEntry chain = null;
            AsymmetricCipherKeyPair privKey = null;
            using (MemoryStream ms = new MemoryStream(pemCertificate))
            {
                PemReader pemReader = new PemReader(new StreamReader(ms));
                object o;
                while ((o = pemReader.ReadObject()) != null)
                {
                    if (o is Org.BouncyCastle.X509.X509Certificate)
                    {
                        chain = new X509CertificateEntry((Org.BouncyCastle.X509.X509Certificate) o);
                    }
                    else if (o is AsymmetricCipherKeyPair)
                    {
                        privKey = (AsymmetricCipherKeyPair) o;
                    }
                }
            }

            if (chain == null || privKey == null)
                return null;
            store.SetKeyEntry("Hyperledger.Fabric", new AsymmetricKeyEntry(privKey.Private), new[] {chain});
            using (MemoryStream ms = new MemoryStream())
            {
                store.Save(ms, "test".ToCharArray(), new SecureRandom());
                ms.Flush();
                ms.Position = 0;
                return new X509Certificate2(ms.ToArray(), "test");
            }

        }

        private List<X509Certificate2> GetX509Certificates(byte[] pemCertificates)
        {
            List<X509Certificate2> certs = new List<X509Certificate2>();
            List<(X509CertificateEntry, AsymmetricCipherKeyPair)> ls = new List<(X509CertificateEntry, AsymmetricCipherKeyPair)>();
            AsymmetricCipherKeyPair privKey = null;
            X509CertificateEntry entry = null;
            using (MemoryStream ms = new MemoryStream(pemCertificates))
            {
                PemReader pemReader = new PemReader(new StreamReader(ms));
                object o;
                while ((o = pemReader.ReadObject()) != null)
                {
                    if (o is Org.BouncyCastle.X509.X509Certificate)
                    {
                        entry = new X509CertificateEntry((Org.BouncyCastle.X509.X509Certificate) o);
                        privKey = null;
                    }
                    else if (o is AsymmetricCipherKeyPair)
                    {
                        privKey = (AsymmetricCipherKeyPair) o;
                    }

                    if (entry != null && privKey != null)
                        ls.Add((entry, privKey));
                }
            }

            foreach ((X509CertificateEntry c, AsymmetricCipherKeyPair p) in ls)
            {
                Pkcs12Store store = new Pkcs12StoreBuilder().Build();
                store.SetKeyEntry("Hyperledger.Fabric", new AsymmetricKeyEntry(p.Private), new[] {c});
                using (MemoryStream ms = new MemoryStream())
                {
                    store.Save(ms, "test".ToCharArray(), new SecureRandom());
                    ms.Flush();
                    ms.Position = 0;
                    certs.Add(new X509Certificate2(ms.ToArray(), "test"));
                }
            }

            return certs;
        }




        /**
         * Return PrivateKey  from pem bytes.
         *
         * @param pemKey pem-encoded private key
         * @return
         */
        public AsymmetricAlgorithm BytesToPrivateKey(byte[] pemKey)
        {
            AsymmetricCipherKeyPair privKey = null;
            using (MemoryStream ms = new MemoryStream(pemKey))
            {
                PemReader pemReader = new PemReader(new StreamReader(ms));
                object o;
                while ((o = pemReader.ReadObject()) != null)
                {
                    if (o is AsymmetricCipherKeyPair)
                    {
                        privKey = (AsymmetricCipherKeyPair) o;
                        break;
                    }
                }
            }



            if (privKey?.Private == null)
                return null;
            if (privKey.Private is RsaKeyParameters)
            {
                RSACryptoServiceProvider sv = new RSACryptoServiceProvider();
                sv.ImportParameters(DotNetUtilities.ToRSAParameters((RsaKeyParameters) privKey.Private));
                return sv;
            }

            if (privKey.Private is DsaPrivateKeyParameters)
            {
                DSACryptoServiceProvider sv = new DSACryptoServiceProvider();
                DsaPrivateKeyParameters kp = (DsaPrivateKeyParameters) privKey.Private;
                DSAParameters p = new DSAParameters();
                p.G = kp.Parameters.G.ToByteArrayUnsigned();
                p.P = kp.Parameters.P.ToByteArrayUnsigned();
                p.Q = kp.Parameters.Q.ToByteArrayUnsigned();
                p.X = kp.X.ToByteArrayUnsigned();
                p.Counter = kp.Parameters.ValidationParameters.Counter;
                p.Seed = kp.Parameters.ValidationParameters.GetSeed();
                sv.ImportParameters(p);
                return sv;
            }

            throw new CryptoException("Unsupported private key");
        }

        public bool Verify(byte[] pemCertificate, string signatureAlgorithm, byte[] signature, byte[] plainText)
        {
            bool isVerified = false;

            if (plainText == null || signature == null || pemCertificate == null)
            {
                return false;
            }

            if (config.ExtraLogLevel(10))
            {
                if (null != diagnosticFileDumper)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("plaintext in hex: " + BitConverter.ToString(plainText).Replace("-", string.Empty));
                    sb.AppendLine("signature in hex: " + BitConverter.ToString(signature).Replace("-", string.Empty));
                    sb.Append("PEM cert in hex: " + BitConverter.ToString(pemCertificate).Replace("-", string.Empty));
                    logger.Trace("verify :  " + diagnosticFileDumper.CreateDiagnosticFile(sb.ToString()));
                }
            }

            try
            {

                X509Certificate2 certificate = GetX509Certificate(pemCertificate);

                if (certificate != null)
                {

                    isVerified = ValidateCertificate(certificate);
                    if (isVerified)
                    {
                        // only proceed if cert is trusted
                        if (certificate.PublicKey.Key is RSACryptoServiceProvider)
                        {
                            RSACryptoServiceProvider prov = (RSACryptoServiceProvider) certificate.PublicKey.Key;
                            HashAlgorithm hs = HashAlgorithm.Create(signatureAlgorithm);
                            if (hs == null)
                            {
                                CryptoException ex = new CryptoException("Cannot verify. Signature algorithm {signatureAlgorithm} is invalid.");
                                logger.ErrorException(ex.Message, ex);
                                throw ex;
                            }

                            isVerified = prov.VerifyData(plainText, hs, signature);
                        }
                        else if (certificate.PublicKey.Key is DSACryptoServiceProvider)
                        {
                            DSACryptoServiceProvider prov = (DSACryptoServiceProvider) certificate.PublicKey.Key;
                            isVerified = prov.VerifyData(plainText, signature);
                        }
                        else if (certificate.PublicKey.Key is ECDsa)
                        {
                            ECDsa prov = (ECDsa) certificate.PublicKey.Key;
                            isVerified = prov.VerifyData(plainText, signature, (HashAlgorithmName) Enum.Parse(typeof(HashAlgorithmName), signatureAlgorithm, true));
                        }
                    }
                }
            }
            catch (CryptoException ee)
            {
                throw ee;
            }
            catch (Exception e)
            {
                CryptoException ex = new CryptoException("Cannot verify signature. " + e.Message + "\r\nCertificate: " + BitConverter.ToString(pemCertificate).Replace("-", string.Empty), e);
                logger.ErrorException(ex.Message, ex);
                throw ex;
            }

            return isVerified;
        } // verify

        private X509Store trustStore = null;

        private void CreateTrustStore()
        {
            try
            {
                X509Store store = new X509Store("Hyperledger.Fabric.Sdk", StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                SetTrustStore(store);

            }
            catch (Exception e)
            {
                throw new CryptoException("Cannot create trust store. Error: " + e.Message, e);
            }
        }

        /**
         * setTrustStore uses the given KeyStore object as the container for trusted
         * certificates
         *
         * @param keyStore the KeyStore which will be used to hold trusted certificates
         * @throws InvalidArgumentException
         */
        private void SetTrustStore(X509Store keyStore)
        {
            trustStore = keyStore ?? throw new InvalidArgumentException("Need to specify a java.security.KeyStore input parameter");
        }

        /**
         * getTrustStore returns the KeyStore object where we keep trusted certificates.
         * If no trust store has been set, this method will create one.
         *
         * @return the trust store as a java.security.KeyStore object
         * @throws CryptoException
         * @see KeyStore
         */
        public X509Store GetTrustStore()
        {
            if (trustStore == null)
            {
                CreateTrustStore();
            }

            return trustStore;
        }

        /**
         * addCACertificateToTrustStore adds a CA cert to the set of certificates used for signature validation
         *
         * @param caCertPem an X.509 certificate in PEM format
         * @param alias     an alias associated with the certificate. Used as shorthand for the certificate during crypto operations
         * @throws CryptoException
         * @throws InvalidArgumentException
         */


        public void AddCACertificateToTrustStore(FileInfo caCertPem, string alias)
        {

            if (caCertPem == null)
            {
                throw new InvalidArgumentException("The certificate cannot be null");
            }

            if (string.IsNullOrEmpty(alias))
            {
                throw new InvalidArgumentException("You must assign an alias to a certificate when adding to the trust store");
            }

            try
            {
                byte[] data = File.ReadAllBytes(caCertPem.FullName);
                X509Certificate2 caCert = BytesToCertificate(data);
                AddCACertificateToTrustStore(caCert, alias);

            }
            catch (Exception e)
            {
                throw new CryptoException("Unable to add CA certificate to trust store. Error: " + e.Message, e);
            }

        }

        /**
         * addCACertificatesToTrustStore adds a CA certs in a stream to the trust store  used for signature validation
         *
         * @param bis an X.509 certificate stream in PEM format in bytes
         * @throws CryptoException
         * @throws InvalidArgumentException
         */
        public void AddCACertificatesToTrustStore(FileInfo caCertPem)
        {

            if (caCertPem == null)
            {
                throw new InvalidArgumentException("The certificate stream bis cannot be null");
            }

            try
            {
                byte[] data = File.ReadAllBytes(caCertPem.FullName);
                if (data.Length == 0)
                {
                    throw new CryptoException("AddCACertificatesToTrustStore: input zero length");
                }

                List<X509Certificate2> caCerts = GetX509Certificates(data);
                foreach (X509Certificate2 caCert in caCerts)
                    AddCACertificateToTrustStore(caCert);

            }
            catch (CertificateException e)
            {
                throw new CryptoException("Unable to add CA certificate to trust store. Error: " + e.Message, e);
            }
        }

        private void AddCACertificateToTrustStore(X509Certificate2 certificate)
        {

            string alias = certificate.SerialNumber ?? certificate.GetHashCode().ToString();
            AddCACertificateToTrustStore(certificate, alias);
        }

        /**
         * addCACertificateToTrustStore adds a CA cert to the set of certificates used for signature validation
         *
         * @param caCert an X.509 certificate
         * @param alias  an alias associated with the certificate. Used as shorthand for the certificate during crypto operations
         * @throws CryptoException
         * @throws InvalidArgumentException
         */
        public void AddCACertificateToTrustStore(X509Certificate2 caCert, string alias)
        {

            if (string.IsNullOrEmpty(alias))
            {
                throw new InvalidArgumentException("You must assign an alias to a certificate when adding to the trust store.");
            }

            if (caCert == null)
            {
                throw new InvalidArgumentException("Certificate cannot be null.");
            }

            try
            {
                if (config.ExtraLogLevel(10))
                {
                    if (null != diagnosticFileDumper)
                    {
                        logger.Trace($"Adding cert to trust store. alias: {alias}" + diagnosticFileDumper.CreateDiagnosticFile(alias + "cert: " + caCert.ToString()));
                    }
                }

                GetTrustStore().Add(caCert);
            }
            catch (Exception e)
            {
                string emsg = "Unable to add CA certificate to trust store. Error: " + e.Message;
                logger.Error(emsg, e);
                throw new CryptoException(emsg, e);
            }
        }


        public void LoadCACertificates(List<X509Certificate2> certificates)
        {
            if (certificates == null || certificates.Count == 0)
            {
                throw new CryptoException("Unable to load CA certificates. List is empty");
            }

            try
            {
                foreach (X509Certificate2 x509Certificate2 in certificates)
                {
                    AddCACertificateToTrustStore(x509Certificate2);
                }
            }
            catch (Exception e)
            {
                // Note: This can currently never happen (as cert<>null and alias<>null)
                throw new CryptoException("Unable to add certificate to trust store. Error: " + e.Message, e);
            }
        }

        /* (non-Javadoc)
         * @see org.hyperledger.fabric.sdk.security.CryptoSuite#loadCACertificatesAsBytes(java.util.Collection)
         */

        public void LoadCACertificatesAsBytes(List<byte[]> certificatesBytes)
        {
            if (certificatesBytes == null || certificatesBytes.Count == 0)
            {
                throw new CryptoException("List of CA certificates is empty. Nothing to load.");
            }

            StringBuilder sb = new StringBuilder();
            List<X509Certificate2> certList = new List<X509Certificate2>();
            foreach (byte[] certBytes in certificatesBytes)
            {
                if (null != diagnosticFileDumper)
                {
                    sb.AppendLine("certificate to load:" + BitConverter.ToString(certBytes).Replace("-", string.Empty));
                }

                certList.Add(BytesToCertificate(certBytes));
            }

            LoadCACertificates(certList);
            if (diagnosticFileDumper != null && sb.Length > 1)
            {
                logger.Trace("loaded certificates: " + diagnosticFileDumper.CreateDiagnosticFile(sb.ToString()));

            }
        }

        /**
         * validateCertificate checks whether the given certificate is trusted. It
         * checks if the certificate is signed by one of the trusted certs in the
         * trust store.
         *
         * @param certPEM the certificate in PEM format
         * @return true if the certificate is trusted
         */
        public bool ValidateCertificate(byte[] certPEM)
        {

            if (certPEM == null)
            {
                return false;
            }

            try
            {

                X509Certificate2 certificate = GetX509Certificate(certPEM);
                if (null == certificate)
                {
                    throw new Exception("Certificate transformation returned null");
                }

                return ValidateCertificate(certificate);
            }
            catch (Exception e)
            {
                logger.Error("Cannot validate certificate. Error is: " + e.Message + "\r\nCertificate (PEM, hex): " + BitConverter.ToString(certPEM).Replace("-", string.Empty));
                return false;
            }
        }

        bool ValidateCertificate(X509Certificate2 cert)
        {
            if (cert == null)
                return false;
            try
            {
                return cert.Verify();
            }
            catch (Exception e)
            {
                logger.Error("Cannot validate certificate. Error is: " + e.Message + "\r\nCertificate" + cert.ToString());
            }

            return false;
        } // validateCertificate

        /**
         * Security Level determines the elliptic curve used in key generation
         *
         * @param securityLevel currently 256 or 384
         * @throws InvalidArgumentException
         */
        public void SetSecurityLevel(int securityLevel)
        {
            logger.Trace($"setSecurityLevel to {securityLevel}", securityLevel);

            if (securityCurveMapping.Count == 0)
            {
                throw new InvalidArgumentException("Security curve mapping has no entries.");
            }

            if (!securityCurveMapping.ContainsKey(securityLevel))
            {
                StringBuilder sb = new StringBuilder();
                string sp = "";
                foreach (int x in securityCurveMapping.Keys)
                {
                    sb.Append(sp).Append(x);
                    sp = ", ";

                }

                throw new InvalidArgumentException($"Illegal security level: {securityLevel}. Valid values are: {sb.ToString()}");
            }

            string lcurveName = securityCurveMapping[securityLevel];

            logger.Debug($"Mapped curve strength {securityLevel} to {lcurveName}");
            X9ECParameters pars = ECNamedCurveTable.GetByName(lcurveName);
            //Check if can match curve name to requested strength.
            if (pars == null)
            {

                InvalidArgumentException invalidArgumentException = new InvalidArgumentException($"Curve {curveName} defined for security strength {securityLevel} was not found.");

                logger.ErrorException(invalidArgumentException.Message, invalidArgumentException);
                throw invalidArgumentException;

            }

            this.curveName = lcurveName;
            this.securityLevel = securityLevel;
        }

        public void SetHashAlgorithm(string algorithm)
        {
            if (string.IsNullOrEmpty(algorithm) || !("SHA2".Equals(algorithm, StringComparison.InvariantCultureIgnoreCase) || "SHA3".Equals(algorithm, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new InvalidArgumentException("Illegal Hash function family: " + algorithm + " - must be either SHA2 or SHA3");
            }

            this.hashAlgorithm = algorithm;
        }

        public AsymmetricAlgorithm KeyGen()
        {
            return EcdsaKeyGen();
        }

        private AsymmetricAlgorithm EcdsaKeyGen()
        {
            return GenerateKey("EC", curveName);
        }

        private AsymmetricAlgorithm GenerateKey(string encryptionName, string curveName)
        {
            try
            {
                ECCurve curve = ECCurve.CreateFromFriendlyName(curveName);
                ECDsa ec = ECDsa.Create(curve);
                return ec;
            }
            catch (Exception exp)
            {
                throw new CryptoException("Unable to generate key pair", exp);
            }
        }

        /**
         * Decodes an ECDSA signature and returns a two element BigInteger array.
         *
         * @param signature ECDSA signature bytes.
         * @return BigInteger array for the signature's r and s values
         * @throws Exception
         */
        private static BigInteger[] DecodeECDSASignature(byte[] signature)
        {


            Asn1InputStream asnInputStream = new Asn1InputStream(signature);
            Asn1Object asn1 = asnInputStream.ReadObject();
            BigInteger[] sigs = new BigInteger[2];
            int count = 0;
            if (asn1 is Asn1Sequence)
            {
                Asn1Sequence asn1Sequence = (Asn1Sequence) asn1;
                foreach (Asn1Encodable asn1Encodable in asn1Sequence)
                {
                    Asn1Object asn1Primitive = asn1Encodable.ToAsn1Object();

                    if (asn1Primitive is DerInteger)
                    {
                        DerInteger asn1Integer = (DerInteger) asn1Primitive;
                        BigInteger integer = asn1Integer.Value;
                        if (count < 2)
                        {
                            sigs[count] = integer;
                        }

                        count++;
                    }
                }
            }

            if (count != 2)
            {
                throw new CryptoException($"Invalid ECDSA signature. Expected count of 2 but got: {count}. Signature is: {BitConverter.ToString(signature).Replace("-", string.Empty)}");
            }

            return sigs;
        }


        /**
         * Sign data with the specified elliptic curve private key.
         *
         * @param privateKey elliptic curve private key.
         * @param data       data to sign
         * @return the signed data.
         * @throws CryptoException
         */
        private byte[] EcdsaSignToBytes(AsymmetricAlgorithm privateKey, byte[] data)
        {
            try
            {
                X9ECParameters pars = ECNamedCurveTable.GetByName(curveName);
                BigInteger curveN = pars.N;
                ECDsa ecdsa = (ECDsa) privateKey;
                byte[] signature = ecdsa.SignData(data, (HashAlgorithmName) Enum.Parse(typeof(HashAlgorithmName), DEFAULT_SIGNATURE_ALGORITHM));
                BigInteger[] sigs = DecodeECDSASignature(signature);

                sigs = PreventMalleability(sigs, curveN);
                using (MemoryStream ms = new MemoryStream())
                {
                    DerSequenceGenerator seq = new DerSequenceGenerator(ms);
                    seq.AddObject(new DerInteger(sigs[0]));
                    seq.AddObject(new DerInteger(sigs[1]));
                    seq.Close();
                    ms.Flush();
                    return ms.ToArray();
                }
            }
            catch (Exception e)
            {
                throw new CryptoException("Could not sign the message using private key", e);
            }

        }

        /**
         * @throws ClassCastException if the supplied private key is not of type {@link ECPrivateKey}.
         */

        public byte[] Sign(AsymmetricAlgorithm privateKey, byte[] data)
        {
            return EcdsaSignToBytes(privateKey, data);
        }

        private BigInteger[] PreventMalleability(BigInteger[] sigs, BigInteger curveN)
        {
            BigInteger cmpVal = curveN.Divide(BigInteger.Two);
            BigInteger sval = sigs[1];

            if (sval.CompareTo(cmpVal) == 1)
            {

                sigs[1] = curveN.Subtract(sval);
            }

            return sigs;
        }

        /**
         * generateCertificationRequest
         *
         * @param subject The subject to be added to the certificate
         * @param pair    Public private key pair
         * @return PKCS10CertificationRequest Certificate Signing Request.
         * @throws OperatorCreationException
         */

        public string GenerateCertificationRequest(String subject, AsymmetricAlgorithm pair)
        {
            try
            {
                IDictionary attrs = new Hashtable();
                attrs.Add(X509Name.CN, "Requested Test Certificate");
                AsymmetricCipherKeyPair p = DotNetUtilities.GetKeyPair(pair);
                ISignatureFactory sf = new Asn1SignatureFactory("SHA256withECDSA", p.Private);
                Pkcs10CertificationRequest csr = new Pkcs10CertificationRequest(sf, new X509Name(new ArrayList(attrs.Keys), attrs), p.Public, null, p.Private);
                return CertificationRequestToPEM(csr);
            }
            catch (Exception e)
            {

                logger.ErrorException(e.Message,e);
                throw new InvalidArgumentException(e);

            }

        }

        /**
         * certificationRequestToPEM - Convert a PKCS10CertificationRequest to PEM
         * format.
         *
         * @param csr The Certificate to convert
         * @return An equivalent PEM format certificate.
         * @throws IOException
         */

        private string CertificationRequestToPEM(Pkcs10CertificationRequest csr)
        {
            PemObject pemCSR = new PemObject("CERTIFICATE REQUEST", csr.GetEncoded());
            StringWriter str = new StringWriter();
            PemWriter pemWriter = new PemWriter(str);
            pemWriter.WriteObject(pemCSR);
            str.Flush();
            str.Close();
            return str.ToString();
        }



        public byte[] Hash(byte[] input)
        {
            IDigest digest = GetHashDigest();
            byte[] retValue = new byte[digest.GetDigestSize()];
            digest.BlockUpdate(input,0,input.Length);
            digest.DoFinal(retValue, 0);
            return retValue;
        }


        public ICryptoSuiteFactory GetCryptoSuiteFactory()
        {
            return HLSDKJCryptoSuiteFactory.Instance; //Factory for this crypto suite.
        }

        private bool inited = false;


        public void Init()
        {
            if (inited)
            {
                throw new InvalidArgumentException("Crypto suite already initialized");
            }

            ResetConfiguration();

        }

        private IDigest GetHashDigest()
        {
            if ("SHA3".Equals(hashAlgorithm, StringComparison.CurrentCultureIgnoreCase))
            {
                return new Sha3Digest();
            }
            else
            {
                // Default to SHA2
                return new Sha256Digest();
            }
        }

        //    /**
        //     * Shake256 hash the supplied byte data.
        //     *
        //     * @param in        byte array to be hashed.
        //     * @param bitLength of the result.
        //     * @return the hashed byte data.
        //     */
        //    public byte[] shake256(byte[] in, int bitLength) {
        //
        //        if (bitLength % 8 != 0) {
        //            throw new IllegalArgumentException("bit length not modulo 8");
        //
        //        }
        //
        //        final int byteLen = bitLength / 8;
        //
        //        SHAKEDigest sd = new SHAKEDigest(256);
        //
        //        sd.update(in, 0, in.length);
        //
        //        byte[] out = new byte[byteLen];
        //
        //        sd.doFinal(out, 0, byteLen);
        //
        //        return out;
        //
        //    }

        /**
         * Resets curve name, hash algorithm and cert factory. Call this method when a config value changes
         *
         * @throws CryptoException
         * @throws InvalidArgumentException
         */
        private void ResetConfiguration()
        {

            SetSecurityLevel(securityLevel);
            SetHashAlgorithm(hashAlgorithm);
        }

//    /* (non-Javadoc)
    //     * @see org.hyperledger.fabric.sdk.security.CryptoSuite#setProperties(java.util.Properties)
    //     */
    //    @Override
        public void SetProperties(Dictionary<string,string> properties)
        {
            if (properties == null || properties.Count==0) {
                throw new InvalidArgumentException("properties must not be null");
            }
            //        if (properties != null) {
            hashAlgorithm = properties.ContainsKey(Config.HASH_ALGORITHM) ? properties[Config.HASH_ALGORITHM] : hashAlgorithm;
            string secLevel = properties.ContainsKey(Config.SECURITY_LEVEL) ? properties[Config.SECURITY_LEVEL] : securityLevel.ToString();
            securityLevel = int.Parse(secLevel);
            if (properties.ContainsKey(Config.SECURITY_CURVE_MAPPING)) {
                securityCurveMapping = Config.ParseSecurityCurveMappings(properties[Config.SECURITY_CURVE_MAPPING]);
            } else {
                securityCurveMapping = config.GetSecurityCurveMapping();
            }
            CERTIFICATE_FORMAT = properties.ContainsKey(Config.CERTIFICATE_FORMAT) ? properties[Config.CERTIFICATE_FORMAT] : CERTIFICATE_FORMAT;
            DEFAULT_SIGNATURE_ALGORITHM = properties.ContainsKey(Config.SIGNATURE_ALGORITHM) ? properties[Config.SIGNATURE_ALGORITHM] : DEFAULT_SIGNATURE_ALGORITHM;
            ResetConfiguration();

        }

        /* (non-Javadoc)
         * @see org.hyperledger.fabric.sdk.security.CryptoSuite#getProperties()
         */

        public Dictionary<string,string> GetProperties()
        {
            Dictionary<string,string> properties= new Dictionary<string, string>();
            properties.Add(Config.HASH_ALGORITHM, hashAlgorithm);
            properties.Add(Config.SECURITY_LEVEL, securityLevel.ToString());
            properties.Add(Config.CERTIFICATE_FORMAT, CERTIFICATE_FORMAT);
            properties.Add(Config.SIGNATURE_ALGORITHM, DEFAULT_SIGNATURE_ALGORITHM);
            return properties;
        }

        public byte[] CertificateToDER(string certificatePEM)
        {

            byte[] content = null;

            try
            {
                PemReader pemReader = new PemReader(new StringReader(certificatePEM));
                PemObject pemObject = pemReader.ReadPemObject();
                content = pemObject.Content;
            }
            catch (Exception e)
            {
                // best attempt
            }
            return content;
        }

    }
}
