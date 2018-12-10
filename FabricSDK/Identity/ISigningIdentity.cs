/*
 *
 *  Copyright IBM Corp. All Rights Reserved.
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *     http://www.apache.org/licenses/LICENSE-2.0
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 */

namespace Hyperledger.Fabric.SDK.Identity
{
    /**
     * SigningIdentity extends Identity with signing capabilities. It is used by a
     * client to sign transactions in a linkable or unlinkable fashion.
     */
    public interface ISigningIdentity : IIdentity
    {

        /**
         * Sings a message with the secret key and the corresponding certificate
         *
         * @param msg
         * @return signature
         * @throws CryptoException
         * @throws InvalidArgumentException
         */
        byte[] Sign(byte[] msg);

        /**
         * Verifies a signature on a message
         *
         * @param msg
         * @param sig
         * @return true/false
         * @throws CryptoException
         * @throws InvalidArgumentException
         */
        bool VerifySignature(byte[] msg, byte[] sig);
    }
}