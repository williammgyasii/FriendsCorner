# The zone was created in the dashboard. Terraform reads it and does not own it.
data "cloudflare_zone" "site" {
  filter = {
    name = var.domain
    account = {
      id = var.account_id
    }
  }
}
