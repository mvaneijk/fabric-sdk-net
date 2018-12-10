/*
 *
 *  Copyright 2017, 2018 IBM Corp. All Rights Reserved.
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

using System;
using System.Linq;
using Google.Protobuf;
using Hyperledger.Fabric.SDK.AMCL;
using Hyperledger.Fabric.SDK.AMCL.FP256BN;
using Org.BouncyCastle.Security;

namespace Hyperledger.Fabric.SDK.Idemix
{
    /**
     * A NonRevocationProver is a prover that can prove that an identity mixer credential is not revoked.
     * For every RevocationAlgorithm, there will be an instantiation of NonRevocationProver.
     */
    public interface INonRevocationVerifier
    {
        /**
         * recomputeFSContribution verifies a non-revocation proof by recomputing the Fiat-Shamir contribution.
         *
         * @param proof     Non revocation proof
         * @param challenge Challenge
         * @param epochPK   Epoch PK
         * @param proofSRh  Proof of revocation handle
         * @return The recomputed FSContribution
         */
        byte[] RecomputeFSContribution(Protos.Idemix.NonRevocationProof proof, BIG challenge, ECP2 epochPK, BIG proofSRh);
    }

    public static class NonRevocationVerifier
    {
        /**
         * This method provides a non-revocation verifier depending on the Revocation algorithm
         *
         * @param algorithm Revocation mechanism to use
         * @return NonRevocationVerifier or null if not allowed
         */
        public static INonRevocationVerifier GetNonRevocationVerifier(RevocationAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case RevocationAlgorithm.ALG_NO_REVOCATION:
                    return new NopNonRevocationVerifier();
                default:
                    // Revocation algorithm not supported
                    throw new Exception("Revocation algorithm " + algorithm.ToString() + " not supported");
            }
        }

    }
}