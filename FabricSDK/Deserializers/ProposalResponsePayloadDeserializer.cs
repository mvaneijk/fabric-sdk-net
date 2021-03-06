/*
 *
 *  Copyright 2016,2017 DTCC, Fujitsu Australia Software Technology, IBM - All Rights Reserved.
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

using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer.FabricProposalResponse;
using Hyperledger.Fabric.SDK.Helper;

namespace Hyperledger.Fabric.SDK.Deserializers
{
    public class ProposalResponsePayloadDeserializer : BaseDeserializer<ProposalResponsePayload>
    {
        private readonly WeakItem<ChaincodeActionDeserializer, ProposalResponsePayload> chaincodeAction;

        public ProposalResponsePayloadDeserializer(ByteString byteString) : base(byteString)
        {
            chaincodeAction=new WeakItem<ChaincodeActionDeserializer, ProposalResponsePayload>((ext) => new ChaincodeActionDeserializer(ext.Extension),()=>Reference);
        }
        public ProposalResponsePayload ProposalResponsePayload => Reference;
        public ChaincodeActionDeserializer Extension => chaincodeAction.Reference;

    }
}
