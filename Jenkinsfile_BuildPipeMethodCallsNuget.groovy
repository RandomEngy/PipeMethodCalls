library(
    identifier: "jenkins-common-lib@v1.5",
    retriever: modernSCM(github(credentialsId: "github-app-dev", repository: "jenkins-common-lib", repoOwner: "coveo")),
    changelog: false
)

import java.text.SimpleDateFormat

def latestVersionStr = ""

pipeline {
    agent {
        node {
            label 'windows'
            customWorkspace "wks\\PipeMethodCallsNugetCreate"
        }
    }
    options {
        ansiColor('xterm')
        buildDiscarder(logRotator(numToKeepStr: "30"))
        disableConcurrentBuilds()
        timeout(time: 2, unit: 'HOURS')
        timestamps()
    }
    parameters {
        string(name: "latestVersion", description: "The value of the version to deploy.")
        booleanParam(name: 'publishNuGetPackage', description: 'Whether to publish NuGet package.', defaultValue: true)
    }
    
    stages {
        stage('Checkout') {
            steps {
                checkout([
                    $class: 'GitSCM',
                    branches: [[name: '*/master']],
                    userRemoteConfigs: [[url: 'https://github.com/coveord/PipeMethodCalls',credentialsId:'github-app-dev']],
                    clean: true
                ])
            }
        }

        stage('Init') {
            steps {
                script {
                    latestVersionStr = params.latestVersion
                    if(latestVersionStr != ""){
                        completeVersion = "${latestVersionStr}"
                    } 
                    
                    currentBuild.displayName = "#${env.BUILD_NUMBER} - v${completeVersion}"
                }
                echo "The version for this build will be: ${completeVersion}"
            }
        }

        stage('Build') {
            steps {
                echo "Building the PipeMethodCalls library."
                script { 
                   bat "dotnet build -c Release"
                }
            }
        }
        
        stage('Publish NuGet Package') {
            when {
                expression { return currentBuild.currentResult == 'SUCCESS' && params.publishNuGetPackage }
            }

            steps {
                script {
                    bat "nuget pack PipeMethodCalls.nuspec -properties version=${completeVersion}"
                    bat "nuget push *.nupkg"
                }
                echo "The nuget package for PipeMethodCalls has been published."
            }
        }
    }

    post {
        cleanup {
            dir ("bin") { deleteDir() }
            dir ("obj") { deleteDir() }
        }
    }
}
