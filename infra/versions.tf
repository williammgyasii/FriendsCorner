terraform {
  required_version = ">= 1.10"

  required_providers {
    cloudflare = {
      source  = "cloudflare/cloudflare"
      version = "~> 5.0"
    }
  }

  # State lives in R2, never in git. The bucket was created by hand because
  # this configuration cannot create the place it stores its own state.
  # Credentials come from AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY (infra/.env).
  backend "s3" {
    bucket = "distancetogether-tfstate"
    key    = "distancetogether/terraform.tfstate"
    region = "auto"

    endpoints = {
      s3 = "https://e23518956f08ff35812d9ab001a39880.r2.cloudflarestorage.com"
    }

    use_lockfile                = true
    use_path_style              = true
    skip_credentials_validation = true
    skip_metadata_api_check     = true
    skip_region_validation      = true
    skip_requesting_account_id  = true
    skip_s3_checksum            = true
  }
}
