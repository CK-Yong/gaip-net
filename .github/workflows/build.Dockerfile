# openjdk-builder and antlr-builder adapted from https://github.com/antlr/antlr4/tree/master/docker
FROM adoptopenjdk/openjdk11:alpine AS openjdk-builder
WORKDIR /opt/antlr4

ARG ANTLR_VERSION="4.9.3"
ARG MAVEN_OPTS="-Xmx1G"

RUN apk add --no-cache maven git \
    && git clone https://github.com/antlr/antlr4.git \
    && cd antlr4 \
    && git checkout $ANTLR_VERSION \
    && mvn clean --projects tool --also-make \
    && mvn -DskipTests install --projects tool --also-make \
    && mv ./tool/target/antlr4-*-complete.jar antlr4-tool.jar

FROM adoptopenjdk/openjdk11:alpine-jre AS antlr-builder
COPY --from=openjdk-builder /opt/antlr4/antlr4/antlr4-tool.jar /usr/local/lib/

FROM antlr-builder AS grammar-builder
WORKDIR /work

COPY ./src/Gaip.Net.Core/Grammar .
RUN java -jar /usr/local/lib/antlr4-tool.jar -Dlanguage=CSharp Filter.g4 -o /output -visitor

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS dotnet-restored
WORKDIR /work

COPY . .
COPY --from=grammar-builder /output /work/src/Gaip.Net.Core/Antlr4
RUN dotnet restore

FROM dotnet-restored AS dotnet-build
RUN dotnet build --no-restore

FROM dotnet-build AS dotnet-test
RUN dotnet test --no-build --verbosity normal --logger trx --results-directory /work/testresults
